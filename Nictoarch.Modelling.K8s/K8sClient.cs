using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Jsonata.Net.Native;
using Jsonata.Net.Native.Json;
using k8s;
using k8s.Authentication;
using k8s.Autorest;
using k8s.Exceptions;
using NLog;

namespace Nictoarch.Modelling.K8s
{
    internal sealed class K8sClient: IDisposable
    {
        private readonly Logger m_logger = LogManager.GetCurrentClassLogger();
        private readonly SocketsHttpHandler m_httpHandler;
        private readonly JsonSerializerOptions m_jsonSerializerOptions;
        private readonly bool m_disableHttp2;
        private readonly HttpClient m_httpClient;
        private readonly Uri m_baseUri;
        private readonly string m_tlsServerName;
        private readonly ServiceClientCredentials m_clientCredentials;

        private IReadOnlyList<ApiInfo>? m_apiInfos = null;

        public IReadOnlyList<ApiInfo> ApiInfos => this.m_apiInfos ?? throw new Exception($"Please call {nameof(this.InitAsync)}() before accessing {nameof(this.ApiInfos)}");

        internal static KubernetesClientConfiguration GetConfiguration(string? configFile, double? httpClientTimeoutSeconds = null)
        {
            KubernetesClientConfiguration config;
            if (configFile != null)
            {
                config = KubernetesClientConfiguration.BuildConfigFromConfigFile(configFile);
            }
            else
            {
                config = KubernetesClientConfiguration.BuildConfigFromConfigFile();
            }

            if (httpClientTimeoutSeconds != null)
            {
                config.HttpClientTimeout = TimeSpan.FromSeconds(httpClientTimeoutSeconds.Value);
            }
            return config;
        }

        internal K8sClient(KubernetesClientConfiguration config)
        {
            if (string.IsNullOrWhiteSpace(config.Host))
            {
                throw new KubeConfigException("Host url must be set");
            }

            try
            {
                this.m_baseUri = new Uri(config.Host);
            }
            catch (UriFormatException e)
            {
                throw new KubeConfigException("Bad host url", e);
            }

            this.m_tlsServerName = config.TlsServerName;

            this.m_httpHandler = new SocketsHttpHandler {
                KeepAlivePingPolicy = HttpKeepAlivePingPolicy.WithActiveRequests,
                KeepAlivePingDelay = TimeSpan.FromMinutes(3),
                KeepAlivePingTimeout = TimeSpan.FromSeconds(30),
                EnableMultipleHttp2Connections = true,
            };

            this.m_httpHandler.SslOptions.ClientCertificates = new X509Certificate2Collection();

            if (this.m_baseUri.Scheme == "https")
            {
                if (config.SkipTlsVerify)
                {
                    this.m_httpHandler.SslOptions.RemoteCertificateValidationCallback =
                        (sender, certificate, chain, sslPolicyErrors) => true;
                }
                else
                {
                    if (config.SslCaCerts != null)
                    {
                        this.m_httpHandler.SslOptions.RemoteCertificateValidationCallback =
                            (sender, certificate, chain, sslPolicyErrors) => CertificateValidationCallBack(config.SslCaCerts, certificate, chain, sslPolicyErrors);
                    }
                }
            }

            this.m_clientCredentials = Kubernetes.CreateCredentials(config);

            X509Certificate2? clientCert = GetClientCert(config);
            if (clientCert != null)
            {
                this.m_httpHandler.SslOptions.ClientCertificates.Add(clientCert);

                // TODO this is workaround for net7.0, remove it when the issue is fixed
                // seems the client certificate is cached and cannot be updated
                this.m_httpHandler.SslOptions.LocalCertificateSelectionCallback = (sender, targetHost, localCertificates, remoteCertificate, acceptableIssuers) => {
                    return clientCert;
                };
            }

            this.m_jsonSerializerOptions = config.JsonSerializerOptions;
            this.m_disableHttp2 = config.DisableHttp2;

            config.FirstMessageHandlerSetup?.Invoke(this.m_httpHandler);

            this.m_httpClient = new HttpClient(this.m_httpHandler, false) {
                Timeout = config.HttpClientTimeout,
            };
        }

        internal async Task InitAsync(CancellationToken cancellationToken)
        {
            this.m_apiInfos = await this.RequestApiInfos(cancellationToken);
        }

        //see https://iximiuz.com/en/posts/kubernetes-api-structure-and-terminology/
        internal async Task<JArray> GetResources(string? apiGroup, string resourceKind, string? @namespace, string? labelSelector, CancellationToken cancellationToken)
        {
            //TODO: use proper way to get plural names, but it looks rather ok for now
            string resourcePlural;
            string resourceSingular;

            //get singular/plural and api group
            {
                ApiInfo? resourceInfo = this.ApiInfos.FirstOrDefault(a => a.resource_singular == resourceKind);
                if (resourceInfo == null)
                {
                    resourceInfo = this.ApiInfos.FirstOrDefault(a => a.resource_plural == resourceKind);
                }

                if (resourceInfo == null)
                {
                    throw new Exception($"Unable to find resource with kind '{resourceKind}'");
                }
                resourceSingular = resourceInfo.resource_singular;
                resourcePlural = resourceInfo.resource_plural;

                if (apiGroup == null)
                {
                    apiGroup = resourceInfo.api_group;
                }
            }

            string relativeUri;


            //https://kubernetes.io/docs/reference/using-api/api-concepts/#resource-uris
            if (apiGroup == "v1" || apiGroup == "core")
            {
                //core resources are in api/v1
                if (@namespace != null)
                {
                    relativeUri = $"api/v1/namespaces/{@namespace}/{resourcePlural}";
                }
                else
                {
                    relativeUri = $"api/v1/{resourcePlural}";
                }
            }
            else
            {

                //TODO: use proper way to get version, but it looks rather ok for now
                string apiGroupVersioned = apiGroup.Contains("/v") ? apiGroup : apiGroup + "/v1";

                if (@namespace != null)
                {
                    relativeUri = $"apis/{apiGroupVersioned}/namespaces/{@namespace}/{resourcePlural}";
                }
                else
                {
                    relativeUri = $"apis/{apiGroupVersioned}/{resourcePlural}";
                }
            }

            if (labelSelector != null)
            {
                relativeUri += "?labelSelector=" + HttpUtility.UrlEncode(labelSelector);
            }

            JToken result = await this.SendRequest(relativeUri, HttpMethod.Get, null, null, cancellationToken);

            //TODO: add better validation
            JObject resultObj = (JObject)result;
            string resultKind = (string)resultObj.Properties["kind"];
            if (resultKind.ToLowerInvariant() != resourceSingular + "list") 
            {
                throw new Exception($"Unexpected result kind: '{resultKind}', expected {(resourceSingular + "list")}");
            }

            JArray resultList = (JArray)resultObj.Properties["items"];
            return resultList;
        }

        private async Task<IReadOnlyList<ApiInfo>> RequestApiInfos(CancellationToken cancellationToken)
        {
            List<ApiInfo> result = new List<ApiInfo>();

            JsonataQuery mainQuery = new JsonataQuery(@"
                $.resources[""list"" in verbs].{
                    ""api_group"": $group,
                    ""resource_singular"": $lowercase(`kind`),
                    ""resource_plural"": `name`,
                    ""namespaced"": `namespaced`
                }[]
            ");

            //check old classic core api
            {
                JToken queryResult = await this.SendRequest("api/v1", HttpMethod.Get, null, null, cancellationToken);

                JObject bindings = new JObject();
                bindings.Set("group", new JValue("v1"));

                JToken transformed = mainQuery.Eval(queryResult, bindings);
                List<ApiInfo> currentResults = transformed.ToObject<List<ApiInfo>>();
                result.AddRange(currentResults);
            }

            //new apis
            {
                JToken groupsJson = await this.SendRequest("apis", HttpMethod.Get, null, null, cancellationToken);
                JsonataQuery groupsQuery = new JsonataQuery(@"$.groups.preferredVersion.groupVersion");
                JToken groupsTransformed = groupsQuery.Eval(groupsJson);
                List<string> groups = groupsTransformed.ToObject<List<string>>();

                foreach (string group in groups)
                {
                    JToken queryResult = await this.SendRequest("apis/" + group, HttpMethod.Get, null, null, cancellationToken);

                    JObject bindings = new JObject();
                    bindings.Set("group", new JValue(group));

                    JToken transformed = mainQuery.Eval(queryResult, bindings);
                    if (transformed.Type == JTokenType.Undefined)
                    {
                        //no match at all
                        continue;
                    }
                    List<ApiInfo> currentResults = transformed.ToObject<List<ApiInfo>>();
                    result.AddRange(currentResults);
                }
            }

            return result;
        }

        private Task<JToken> SendRequest(string relativeUri, HttpMethod method, IReadOnlyDictionary<string, IReadOnlyList<string>>? customHeaders, object? body, CancellationToken cancellationToken)
        {
            using (HttpRequestMessage httpRequest = new HttpRequestMessage {
                Method = method,
                RequestUri = new Uri(this.m_baseUri, relativeUri),
            })
            {
                if (!this.m_disableHttp2)
                {
                    httpRequest.Version = HttpVersion.Version20;
                }

                if (customHeaders != null)
                {
                    foreach (KeyValuePair<string, IReadOnlyList<string>> header in customHeaders)
                    {
                        httpRequest.Headers.Remove(header.Key);
                        httpRequest.Headers.TryAddWithoutValidation(header.Key, header.Value);
                    }
                }

                if (body != null)
                {
                    string requestContent = KubernetesJson.Serialize(body, this.m_jsonSerializerOptions);
                    httpRequest.Content = new StringContent(requestContent, Encoding.UTF8);
                    httpRequest.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json; charset=utf-8");
                }

                //this.m_logger.Trace($"{httpRequest.Method} {httpRequest.RequestUri} ({httpRequest.Version}, {httpRequest.Content?.Headers.ContentType}) ");

                return SendRequestRaw(httpRequest, cancellationToken);
            }
        }

        private async Task<JToken> SendRequestRaw(HttpRequestMessage httpRequest, CancellationToken cancellationToken)
        {
            // Set Credentials
            if (this.m_clientCredentials != null)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await this.m_clientCredentials.ProcessHttpRequestAsync(httpRequest, cancellationToken);
            }

            if (!string.IsNullOrWhiteSpace(this.m_tlsServerName))
            {
                httpRequest.Headers.Host = this.m_tlsServerName;
            }

            // Send Request
            cancellationToken.ThrowIfCancellationRequested();
            using (HttpResponseMessage httpResponse = await this.m_httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken))
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!httpResponse.IsSuccessStatusCode)
                {
                    string responseContent;
                    try
                    {
                        if (httpResponse.Content != null)
                        {
                            responseContent = await httpResponse.Content.ReadAsStringAsync();
                        }
                        else
                        {
                            responseContent = string.Empty;
                        }
                    }
                    catch (Exception)
                    {
                        responseContent = string.Empty;
                    }

                    throw new HttpException($"Error executing request: {(int)httpResponse.StatusCode} {httpResponse.StatusCode}: {responseContent}", httpResponse.StatusCode);
                }

                //TODO: see https://github.com/mikhail-barg/jsonata.net.native/issues/12
                //      see https://github.com/dotnet/runtime/issues/68983
                /*
                using (Stream stream = await httpResponse.Content.ReadAsStreamAsync(cancellationToken))
                using (TextReader reader = new StreamReader(stream))
                {
                    JToken result = JToken.Parse(reader);
                    return result;
                }
                */
                string responseStr = await httpResponse.Content.ReadAsStringAsync(cancellationToken);
                JToken result = JToken.Parse(responseStr);
                return result;
            }
        }

        private static X509Certificate2? GetClientCert(KubernetesClientConfiguration config)
        {
            if ((!string.IsNullOrWhiteSpace(config.ClientCertificateData) ||
                 !string.IsNullOrWhiteSpace(config.ClientCertificateFilePath)) &&
                (!string.IsNullOrWhiteSpace(config.ClientCertificateKeyData) ||
                 !string.IsNullOrWhiteSpace(config.ClientKeyFilePath)))
            {
                return GeneratePfx(config);
            }

            return null;
        }

        public static X509Certificate2 GeneratePfx(KubernetesClientConfiguration config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            string keyData;
            string certData;

            if (!string.IsNullOrWhiteSpace(config.ClientCertificateKeyData))
            {
                keyData = Encoding.UTF8.GetString(Convert.FromBase64String(config.ClientCertificateKeyData));
            }
            else if (!string.IsNullOrWhiteSpace(config.ClientKeyFilePath))
            {
                keyData = File.ReadAllText(config.ClientKeyFilePath);
            }
            else 
            {
                throw new KubeConfigException("keyData is empty");
            }

            if (!string.IsNullOrWhiteSpace(config.ClientCertificateData))
            {
                certData = Encoding.UTF8.GetString(Convert.FromBase64String(config.ClientCertificateData));
            }
            else if (!string.IsNullOrWhiteSpace(config.ClientCertificateFilePath))
            {
                certData = File.ReadAllText(config.ClientCertificateFilePath);
            }
            else 
            {
                throw new KubeConfigException("certData is empty");
            }


            X509Certificate2 cert = X509Certificate2.CreateFromPem(certData, keyData);

            // see https://github.com/kubernetes-client/csharp/issues/737
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                if (config.ClientCertificateKeyStoreFlags.HasValue)
                {
                    cert = new X509Certificate2(cert.Export(X509ContentType.Pkcs12), "", config.ClientCertificateKeyStoreFlags.Value);
                }
                else
                {
                    cert = new X509Certificate2(cert.Export(X509ContentType.Pkcs12));
                }
            }

            return cert;
        }

        private static bool CertificateValidationCallBack(
            X509Certificate2Collection caCerts,
            X509Certificate? certificate,
            X509Chain? chain,
            SslPolicyErrors sslPolicyErrors
        )
        {
            if (caCerts == null)
            {
                throw new ArgumentNullException(nameof(caCerts));
            }

            if (chain == null)
            {
                throw new ArgumentNullException(nameof(chain));
            }

            // If the certificate is a valid, signed certificate, return true.
            if (sslPolicyErrors == SslPolicyErrors.None)
            {
                return true;
            }

            // If there are errors in the certificate chain, look at each error to determine the cause.
            if ((sslPolicyErrors & SslPolicyErrors.RemoteCertificateChainErrors) != 0)
            {
                chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;

                // Added our trusted certificates to the chain
                //
                chain.ChainPolicy.ExtraStore.AddRange(caCerts);

                chain.ChainPolicy.VerificationFlags = X509VerificationFlags.AllowUnknownCertificateAuthority;
                bool isValid = chain.Build((X509Certificate2)certificate!);

                bool isTrusted = false;

                // Make sure that one of our trusted certs exists in the chain provided by the server.
                //
                foreach (X509Certificate2 cert in caCerts)
                {
                    if (chain.Build(cert))
                    {
                        isTrusted = true;
                        break;
                    }
                }

                return isValid && isTrusted;
            }

            // In all other cases, return false.
            return false;
        }

        public void Dispose()
        {
            this.m_httpClient.Dispose();
            this.m_httpHandler.Dispose();
        }
    }
}
