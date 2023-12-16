using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Jsonata.Net.Native.Json;
using Nictoarch.Common.Xml2Json;
using Nictoarch.Modelling.Core;
using Nictoarch.Modelling.Core.Elements;
using Nictoarch.Modelling.Core.Yaml;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.BufferedDeserialization;
using YamlDotNet.Serialization.BufferedDeserialization.TypeDiscriminators;
using static Nictoarch.Modelling.Json.JsonExtractConfig;
using static Nictoarch.Modelling.Json.JsonSourceConfig;

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
            if (sourceConfig.location.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || sourceConfig.location.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                using (HttpClient httpClient = new HttpClient())
                using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, sourceConfig.location))
                {
                    if (sourceConfig.auth != null)
                    {
                        request.Headers.Authorization = sourceConfig.auth.CreateHeader();
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
            else
            {
                using (Stream fileStream = new FileStream(sourceConfig.location, FileMode.Open, FileAccess.Read))
                {
                    return await this.ReadStream(fileStream, cancellationToken);
                }
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


        /*
        async Task<IModelProvider> IModelProviderFactory<ProviderConfig, QuerySelector, QuerySelector>.GetProviderAsync(ProviderConfig config, CancellationToken cancellationToken)
        {
            JToken json;
            try
            {
                json = await ReadJson(config.source, config.auth, config.source_transform, cancellationToken);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to read source json: {ex.Message}", ex);
            }

            return new JsonModelProvider(json);
        }

        private async Task<JToken> ReadJson(string source, ProviderConfig.Auth? auth, ProviderConfig.ESourceTransform sourceTransform, CancellationToken cancellationToken)
        {
            if (source.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || source.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                using (HttpClient httpClient = new HttpClient())
                using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, source))
                {
                    if (auth != null)
                    {
                        request.Headers.Authorization = auth.CreateHeader();
                    }
                    using (HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken))
                    {
                        response.EnsureSuccessStatusCode();
                        using (Stream responseStream = await response.Content.ReadAsStreamAsync())
                        {
                            return await ProcessSourceStream(responseStream, sourceTransform, cancellationToken);
                        }
                    }
                }
            }
            else
            {
                using (Stream fileStream = new FileStream(source, FileMode.Open, FileAccess.Read))
                {
                    return await ProcessSourceStream(fileStream, sourceTransform, cancellationToken);
                }
            }
        }

        private Task<JToken> ProcessSourceStream(Stream sourceStream, ProviderConfig.ESourceTransform sourceTransform, CancellationToken cancellationToken)
        {
            return sourceTransform switch {
                ProviderConfig.ESourceTransform.none => ParseJsonStream(sourceStream, cancellationToken),
                ProviderConfig.ESourceTransform.xml2json => TransformXml2Json(sourceStream, cancellationToken),
                _ => throw new Exception("Unexpected transform " + sourceTransform),
            };
        }

        private Task<JToken> ParseJsonStream(Stream sourceStream, CancellationToken cancellationToken)
        {
            using (StreamReader reader = new StreamReader(sourceStream))
            {
                JToken result = JToken.Parse(reader);
                return Task.FromResult(result);
            }
        }
        */

        private async Task<JToken> TransformXml2Json(Stream sourceStream, CancellationToken cancellationToken)
        {
            XDocument xml = await XDocument.LoadAsync(sourceStream, LoadOptions.PreserveWhitespace, cancellationToken);
            Xml2JsonConverter converter = new Xml2JsonConverter();
            return converter.Convert(xml);
        }
    }
}