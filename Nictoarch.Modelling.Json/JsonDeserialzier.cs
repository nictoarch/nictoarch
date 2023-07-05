using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jsonata.Net.Native.Json;
using Nictoarch.Modelling.Core.Elements;

namespace Nictoarch.Modelling.Json
{
    internal static class JsonDeserialzier
    {
        private static readonly string[] s_entityProperties = new string[] {
            nameof(Entity.type),
            nameof(Entity.domain_id),
            nameof(Entity.semantic_id),
            nameof(Entity.display_name)
        };

        internal static Entity EntityFromJson(JToken json)
        {
            try
            {
                if (json.Type != JTokenType.Object)
                {
                    throw new Exception($"Json source should be an {nameof(JTokenType.Object)}. Provided {json.Type}.");
                }
                JObject sourceObj = (JObject)json;

                string type = GetStringFromJson(sourceObj, nameof(Entity.type));
                string domainId = GetStringFromJson(sourceObj, nameof(Entity.domain_id));
                string semanticId = GetStringFromJson(sourceObj, nameof(Entity.semantic_id));
                string displayName = GetStringFromJson(sourceObj, nameof(Entity.display_name));

                IEnumerable<string> unusedProperties = sourceObj.Keys.Except(s_entityProperties);
                if (unusedProperties.Any())
                {
                    throw new Exception($"Unexpected properties: {String.Join(", ", unusedProperties)}");
                }

                return new Entity(type, domainId, semanticId, displayName);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to deserialzie {nameof(Entity)}: {ex.Message} Source json:\n{json.ToIndentedString()}", ex);
            }
        }

        private static string GetStringFromJson(JObject source, string propertyName)
        {
            if (!source.Properties.TryGetValue(propertyName, out JToken? value))
            {
                throw new Exception($"Missing '{propertyName}' property");
            }
            if (value.Type != JTokenType.String)
            {
                throw new Exception($"'{propertyName}' should be {nameof(JTokenType.String)}. Provided {value.Type}.");
            }
            return (string)value;
        }
    }
}
