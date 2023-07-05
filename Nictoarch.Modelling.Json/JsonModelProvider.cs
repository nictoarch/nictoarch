using System;
using System.Collections.Generic;
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
using Nictoarch.Modelling.Json;

namespace Nictoarch.Modelling.Drawio
{
    public sealed class JsonModelProvider : IEntityProvider<EntityProviderSpec>
    {
        string IProviderBase.Name => "json";

        async Task<List<Entity>> IEntityProvider<EntityProviderSpec>.GetEntitiesAsync(EntityProviderSpec spec, CancellationToken cancellationToken)
        {
            JToken json;
            try
            {
                json = await ReadJson(spec.source, spec.source_transform, cancellationToken);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to read source json: {ex.Message}", ex);
            }

            JToken resultToken;
            try
            {
                resultToken = spec.entityQuery.Eval(json);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to execure JSONata query: {ex.Message}", ex);
            }

            if (resultToken.Type != JTokenType.Array)
            {
                throw new Exception($"JSONata query for entity selector should return array (of entities), but it returned {resultToken.Type}:\n{resultToken.ToIndentedString()}");
            }

            cancellationToken.ThrowIfCancellationRequested();

            JArray resultArray = (JArray)resultToken;
            List<Entity> entities = new List<Entity>(resultArray.Count);
            foreach (JToken token in resultArray.ChildrenTokens)
            {
                entities.Add(JsonDeserialzier.EntityFromJson(token));
                cancellationToken.ThrowIfCancellationRequested();
            }

            return entities;
        }

        private static async Task<JToken> ReadJson(string source, EntityProviderSpec.ESourceTransform sourceTransform, CancellationToken cancellationToken)
        {
            if (source.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || source.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                using (HttpClient httpClient = new HttpClient()) 
                {
                    using (HttpResponseMessage response = await httpClient.GetAsync(source, cancellationToken))
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

        private static Task<JToken> ProcessSourceStream(Stream sourceStream, EntityProviderSpec.ESourceTransform sourceTransform, CancellationToken cancellationToken)
        {
            return sourceTransform switch {
                EntityProviderSpec.ESourceTransform.none => ParseJsonStream(sourceStream, cancellationToken),
                EntityProviderSpec.ESourceTransform.xml2json => TransformXml2Json(sourceStream, cancellationToken),
                _ => throw new Exception("Unexpected transform " + sourceTransform),
            };
        }

        private static Task<JToken> ParseJsonStream(Stream sourceStream, CancellationToken cancellationToken)
        {
            using (StreamReader reader = new StreamReader(sourceStream))
            {
                JToken result = JToken.Parse(reader);
                return Task.FromResult(result);
            }
        }

        private static async Task<JToken> TransformXml2Json(Stream sourceStream, CancellationToken cancellationToken)
        {
            XDocument xml = await XDocument.LoadAsync(sourceStream, LoadOptions.PreserveWhitespace, cancellationToken);
            Xml2JsonConverter converter = new Xml2JsonConverter();
            return converter.Convert(xml);
        }
    }
}