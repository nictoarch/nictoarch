using System.Threading.Tasks;
using System.Xml.Linq;
using Nictoarch.Modelling.Core;
using YamlDotNet.Serialization;

namespace Nictoarch.Modelling.Db
{
    /*
    public sealed class DbModelProviderFactory : IModelProviderFactory<ProviderConfig, QuerySelector, QuerySelector>
    {
        string IModelProviderFactory.Name => "db";

        void IModelProviderFactory.ConfigureYamlDeserialzier(DeserializerBuilder builder)
        {
            //nothing to do
        }

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

        private async Task<JToken> TransformXml2Json(Stream sourceStream, CancellationToken cancellationToken)
        {
            XDocument xml = await XDocument.LoadAsync(sourceStream, LoadOptions.PreserveWhitespace, cancellationToken);
            Xml2JsonConverter converter = new Xml2JsonConverter();
            return converter.Convert(xml);
        }
    }
    */
}
