using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jsonata.Net.Native.Json;
using Jsonata.Net.Native;
using Nictoarch.Modelling.Core.Elements;

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
            m_query = query;
        }

        public override List<Entity> GetEntities(JToken extractedData)
        {
            JToken queryResult;
            try
            {
                queryResult = m_query.Eval(extractedData);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to eval JsonataQuery: {ex.Message}. (The query was: {m_query})", ex);
            }
            List<Entity> result = new List<Entity>();
            switch (queryResult.Type)
            {
            case JTokenType.Undefined:
                break;
            case JTokenType.Object:
                result.Add(ToEntity(queryResult));
                break;
            case JTokenType.Array:
                foreach (JToken child in ((JArray)queryResult).ChildrenTokens)
                {
                    result.Add(ToEntity(child));
                }
                break;
            default:
                throw new Exception("Entity query should result in a single object or object array, but it returned " + queryResult.Type);
            }
            return result;
        }

        private Entity ToEntity(JToken token)
        {
            if (token.Type != JTokenType.Object)
            {
                throw new Exception($"Attemptiong to convert a JSON {token.Type} to Entity. Should be a JSON Object");
            }
            JObject obj = (JObject)token;
            Entity entity = obj.ToObject<Entity>();
            entity.Validate();
            return entity;
        }
    }

    public sealed class EntitiesSelectorQueryPerField : EntitiesSelectorBase
    {
        [Required] public JsonataQuery type { get; set; } = default!;
        [Required] public JsonataQuery semantic_id { get; set; } = default!;
        public JsonataQuery? domain_id { get; set; }
        public JsonataQuery? display_name { get; set; }

        public override List<Entity> GetEntities(JToken extractedData)
        {
            List<Entity> result = new List<Entity>();
            switch (extractedData.Type)
            {
            case JTokenType.Undefined:
                break;
            case JTokenType.Object:
                result.Add(ToEntity(extractedData));
                break;
            case JTokenType.Array:
                foreach (JToken child in ((JArray)extractedData).ChildrenTokens)
                {
                    result.Add(ToEntity(child));
                }
                break;
            default:
                throw new Exception("Extract should result in a single object or object array, but it returned " + extractedData.Type);
            }
            return result;
        }

        private Entity ToEntity(JToken resource)
        {
            string typeValue = EvaluateValueExpression(resource, type, nameof(type));
            string semanticIdValue = EvaluateValueExpression(resource, semantic_id, nameof(semantic_id));
            string domainIdValue = domain_id != null ? EvaluateValueExpression(resource, domain_id, nameof(domain_id)) : semanticIdValue;
            string displayNameValue = display_name != null ? EvaluateValueExpression(resource, display_name, nameof(display_name)) : semanticIdValue;

            Entity entity = new Entity() {
                type = typeValue,
                domain_id = domainIdValue,
                semantic_id = semanticIdValue,
                display_name = displayNameValue
            };
            entity.Validate();
            return entity;
        }

        private string EvaluateValueExpression(JToken objectTree, JsonataQuery query, string expressionName)
        {
            JToken result;
            try
            {
                result = query.Eval(objectTree);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to execute entity expression query '{expressionName}' ('{query}'): {ex.Message}", ex);
            }

            switch (result.Type)
            {
            case JTokenType.Undefined:
            case JTokenType.Null:
                throw new Exception($"Entity expression query '{expressionName}' ('{query}') returned a non-value ({result.Type})");
            case JTokenType.String:
                return (string)result;
            default:
                return result.ToFlatString();
            }
        }
    }
}
