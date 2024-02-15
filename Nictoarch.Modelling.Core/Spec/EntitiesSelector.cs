using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jsonata.Net.Native.Json;
using Jsonata.Net.Native;
using Nictoarch.Modelling.Core.Elements;
using Nictoarch.Modelling.Core.Jsonata;
using System.Collections;

namespace Nictoarch.Modelling.Core.Spec
{
    public abstract class EntitiesSelectorBase
    {
        public abstract List<Entity> GetEntities(JToken extractedData);

        //needed to parse scalars
        public static EntitiesSelectorBase Parse(string v)
        {
            return new EntitiesSelectorSingleQuery(new JsonataQuery(v));
        }
    }

    public sealed class EntitiesSelectorSingleQuery : EntitiesSelectorBase
    {
        private readonly JsonataQuery m_query;

        internal EntitiesSelectorSingleQuery(JsonataQuery query)
        {
            this.m_query = query;
        }

        public override List<Entity> GetEntities(JToken extractedData)
        {
            JToken queryResult = this.m_query.Eval(extractedData, "entities");
            List<Entity> result = new List<Entity>();
            switch (queryResult.Type)
            {
            case JTokenType.Undefined:
                break;
            case JTokenType.Object:
                result.Add(this.ToEntity(queryResult));
                break;
            case JTokenType.Array:
                foreach (JToken child in ((JArray)queryResult).ChildrenTokens)
                {
                    result.Add(this.ToEntity(child));
                }
                break;
            default:
                throw new JsonataEvalException("Entity query should result in a single object or object array, but it returned " + queryResult.Type, this.m_query, extractedData);
            }
            return result;
        }

        private static readonly string[] ENTITY_KEYS = [
            nameof(Entity.type), 
            nameof(Entity.id), 
            nameof(Entity.display_name), 
            nameof(Entity.properties)
        ];

        private Entity ToEntity(JToken token)
        {
            try
            {
                if (token.Type != JTokenType.Object)
                {
                    throw new Exception($"Attemptiong to convert a JSON {token.Type} to Entity. Should be a JSON Object");
                }

                JObject obj = (JObject)token;
                string type = obj.GetString(nameof(Entity.type));
                string id = obj.GetString(nameof(Entity.id));
                string? displayName = obj.GetStringNullable(nameof(Entity.display_name));
                Entity entity = new Entity() {
                    type = type,
                    id = id,
                    display_name = displayName,
                };
                if (obj.Properties.TryGetValue(nameof(Entity.properties), out JToken? propsToken))
                {
                    entity.properties = propsToken.ToObject<Dictionary<string, object>>();
                }

                IEnumerable<string> extraKeys = obj.Keys.Except(ENTITY_KEYS);
                if (extraKeys.Any())
                {
                    throw new Exception($"Unexpected Entity keys: {String.Join(", ", extraKeys)}. Known keys are: {String.Join(", ", ENTITY_KEYS)}");
                }

                entity.Validate();
                return entity;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to convert JSON to Entity: {ex.Message}.\n----\n{token.ToIndentedString()}", ex);
            }
        }
    }

    public sealed class EntitiesSelectorQueryPerField : EntitiesSelectorBase
    {
        [Required] public JsonataQuery type { get; set; } = default!;
        [Required] public JsonataQuery id { get; set; } = default!;
        public JsonataQuery? display_name { get; set; }
        public JsonataQuery? properties { get; set; }

        public override List<Entity> GetEntities(JToken extractedData)
        {
            List<Entity> result = new List<Entity>();
            switch (extractedData.Type)
            {
            case JTokenType.Undefined:
                break;
            case JTokenType.Object:
                result.Add(this.ToEntity(extractedData));
                break;
            case JTokenType.Array:
                foreach (JToken child in ((JArray)extractedData).ChildrenTokens)
                {
                    result.Add(this.ToEntity(child));
                }
                break;
            default:
                throw new Exception("Extract should result in a single object or object array, but it returned " + extractedData.Type);
            }
            return result;
        }

        private Entity ToEntity(JToken resource)
        {
            string typeValue = this.EvaluateValueExpression(resource, this.type, nameof(this.type));
            string idValue = this.EvaluateValueExpression(resource, this.id, nameof(this.id));
            string? displayNameValue = this.display_name != null ? this.EvaluateValueExpression(resource, this.display_name, nameof(this.display_name)) : null;

            Entity entity = new Entity() {
                type = typeValue,
                id = idValue,
                display_name = displayNameValue
            };

            if (this.properties != null)
            {
                JToken propsToken = this.properties.Eval(resource, nameof(this.properties));
                entity.properties = propsToken.ToObject<Dictionary<string, object>>();
            }

            entity.Validate();
            return entity;
        }

        private string EvaluateValueExpression(JToken objectTree, JsonataQuery query, string expressionName)
        {
            JToken result = query.Eval(objectTree, expressionName);

            switch (result.Type)
            {
            case JTokenType.Undefined:
            case JTokenType.Null:
                throw new JsonataEvalException($"Entity expression query '{expressionName}' returned a non-value ({result.Type})", query, objectTree);
            case JTokenType.String:
                return (string)result;
            default:
                return result.ToFlatString();
            }
        }
    }
}
