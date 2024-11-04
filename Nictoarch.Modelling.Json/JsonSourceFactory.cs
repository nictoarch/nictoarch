using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
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
                string path = Path.Combine(sourceConfig.base_path.path, sourceConfig.location);
                using (Stream fileStream = new FileStream(path, FileMode.Open, FileAccess.Read))
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

    }
}