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
                string semanticId = obj.GetString(nameof(Entity.semantic_id));
                string domainId = obj.GetString(nameof(Entity.domain_id), semanticId);
                string displayName = obj.GetString(nameof(Entity.display_name), semanticId);
                Entity entity = new Entity() {
                    type = type,
                    semantic_id = semanticId,
                    domain_id = domainId,
                    display_name = displayName,
                };
                if (obj.Properties.TryGetValue(nameof(Entity.properties), out JToken? propsToken))
                {
                    entity.properties = propsToken.ToObject<Dictionary<string, object>>();
                }
                //TODO: check unused tokens?
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
        [Required] public JsonataQuery semantic_id { get; set; } = default!;
        public JsonataQuery? domain_id { get; set; }
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
            string semanticIdValue = this.EvaluateValueExpression(resource, this.semantic_id, nameof(this.semantic_id));
            string domainIdValue = this.domain_id != null ? this.EvaluateValueExpression(resource, this.domain_id, nameof(this.domain_id)) : semanticIdValue;
            string displayNameValue = this.display_name != null ? this.EvaluateValueExpression(resource, this.display_name, nameof(this.display_name)) : semanticIdValue;

            Entity entity = new Entity() {
                type = typeValue,
                domain_id = domainIdValue,
                semantic_id = semanticIdValue,
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
