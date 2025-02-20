using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Nictoarch.Modelling.Core;
using Nictoarch.Modelling.Core.Yaml;
using YamlDotNet.Serialization.BufferedDeserialization.TypeDiscriminators;

namespace Nictoarch.Modelling.Json
{
    public sealed class JsonSourceFactory : ISourceFactory<JsonSourceConfig, JsonSource, JsonExtractConfig>
    {
        string ISourceFactory.Name => "json";

        IEnumerable<ITypeDiscriminator> ISourceFactory.GetYamlTypeDiscriminators()
        {
            //see https://github.com/aaubry/YamlDotNet/wiki/Deserialization---Type-Discriminators#determining-type-based-on-the-value-of-a-key

            yield return new StrictKeyValueTypeDiscriminator(
                baseType: typeof(JsonSourceConfig.Auth),
                targetKey: nameof(JsonSourceConfig.Auth.type),
                typeMapping: new Dictionary<string, Type> {
                    { JsonSourceConfig.NoneAuth.TYPE, typeof(JsonSourceConfig.NoneAuth) },
                    { JsonSourceConfig.BasicAuth.TYPE, typeof(JsonSourceConfig.BasicAuth) },
                }
            );
        }

        async Task<ISource> ISourceFactory<JsonSourceConfig, JsonSource, JsonExtractConfig>.GetSource(JsonSourceConfig sourceConfig, CancellationToken cancellationToken)
        {
            string sourceDataString = await this.GetSourceDataString(sourceConfig, cancellationToken);
            return new JsonSource(sourceDataString);
        }

        private async Task<string> GetSourceDataString(JsonSourceConfig sourceConfig, CancellationToken cancellationToken)
        {
            if (sourceConfig.http != null)
            {
                try
                {
                    using SocketsHttpHandler httpHandler = new SocketsHttpHandler();
                    if (!sourceConfig.http.validate_server_certificate)
                    {
                        httpHandler.SslOptions.RemoteCertificateValidationCallback = (object sender, X509Certificate? certificate, X509Chain? chain, SslPolicyErrors sslPolicyErrors) => true;
                    }
                    using (HttpClient httpClient = new HttpClient(httpHandler, disposeHandler: true))
                    using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, sourceConfig.http.url))
                    {
                        if (sourceConfig.http.auth != null)
                        {
                            request.Headers.Authorization = sourceConfig.http.auth.CreateHeader();
                        }
                        if (sourceConfig.http.http_timeout_seconds != null)
                        {
                            httpClient.Timeout = TimeSpan.FromSeconds(sourceConfig.http.http_timeout_seconds.Value);
                        }
                        using (HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken))
                        {
                            response.EnsureSuccessStatusCode();
                            using (Stream responseStream = await response.Content.ReadAsStreamAsync(cancellationToken))
                            {
                                return await this.ReadStream(responseStream, cancellationToken);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception($"Error getting data from {sourceConfig.http.url}: {ex.Message}", ex);
                }
            }
            else if (sourceConfig.file != null)
            {
                if (sourceConfig.file.base_path?.path == null)
                {
                    throw new InvalidOperationException($"Should not happen! {nameof(BasePathAutoProperty)} did not initialize {nameof(JsonSourceConfig.FileSource)}.{nameof(JsonSourceConfig.FileSource.base_path)}");
                }

                string path = Path.Combine(sourceConfig.file.base_path.path, sourceConfig.file.path);
                using (Stream fileStream = new FileStream(path, FileMode.Open, FileAccess.Read))
                {
                    return await this.ReadStream(fileStream, cancellationToken);
                }
            }
            else if (sourceConfig.inplace != null)
            {
                return sourceConfig.inplace.value;
            }
            else
            {
                throw new InvalidOperationException($"Should not happen, check {nameof(JsonSourceConfig.OnDeserialized)}()");
            }
        }

        private async Task<string> ReadStream(Stream sourceStream, CancellationToken cancellationToken)
        {
            using (StreamReader reader = new StreamReader(sourceStream))
            {
                string result = await reader.ReadToEndAsync(cancellationToken);
                return result;
            }
        }

    }
}