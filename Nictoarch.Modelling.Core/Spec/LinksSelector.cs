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
    public abstract class LinksSelectorBase
    {
        public abstract List<Link> GetLinks(JToken extractedData);

        //needed to parse scalars
        public static LinksSelectorBase Parse(string v)
        {
            return new LinksSelectorSingleQuery(new JsonataQuery(v));
        }
    }

    public sealed class LinksSelectorSingleQuery : LinksSelectorBase
    {
        private static readonly IReadOnlyList<string> LINK_KEYS = GetLinkKeys();

        private static List<string> GetLinkKeys()
        {
            return typeof(Link)
                .GetProperties()
                .Select(pi => pi.Name)
                .ToList();
        }

        private readonly JsonataQuery m_query;

        internal LinksSelectorSingleQuery(JsonataQuery query)
        {
            this.m_query = query;
        }

        public override List<Link> GetLinks(JToken extractedData)
        {
            JToken queryResult = this.m_query.Eval(extractedData, "links");
            List<Link> result = new List<Link>();
            switch (queryResult.Type)
            {
            case JTokenType.Undefined:
                break;
            case JTokenType.Object:
                result.Add(this.ToLink(queryResult));
                break;
            case JTokenType.Array:
                foreach (JToken child in ((JArray)queryResult).ChildrenTokens)
                {
                    result.Add(this.ToLink(child));
                }
                break;
            default:
                throw new JsonataEvalException("Link query should result in a single object or object array, but it returned " + queryResult.Type, this.m_query, extractedData);
            }
            return result;
        }

        

        private Link ToLink(JToken token)
        {
            try
            {
                if (token.Type != JTokenType.Object)
                {
                    throw new Exception($"Attemptiong to convert a JSON {token.Type} to Link. Should be a JSON Object");
                }

                JObject obj = (JObject)token;
                string type = obj.GetString(nameof(Link.type));
                string id = obj.GetString(nameof(Link.id));
                string? displayName = obj.GetStringNullable(nameof(Link.display_name));
                EntityKey from = obj.GetEntityKey(nameof(Link.from));
                EntityKey to = obj.GetEntityKey(nameof(Link.to));
                Link link = new Link() {
                    type = type,
                    id = id,
                    display_name = displayName,
                    from = from,
                    to = to,
                };
                if (obj.Properties.TryGetValue(nameof(Link.properties), out JToken? propsToken))
                {
                    link.properties = propsToken.ToObject<Dictionary<string, object>>();
                }
                if (obj.Properties.TryGetValue(nameof(Link.properties_info), out JToken? propsInfoToken))
                {
                    link.properties_info = propsInfoToken.ToObject<Dictionary<string, object>>();
                }

                IEnumerable<string> extraKeys = obj.Keys.Except(LINK_KEYS);
                if (extraKeys.Any())
                {
                    throw new Exception($"Unexpected Link keys: {String.Join(", ", extraKeys)}. Known keys are: {String.Join(", ", LINK_KEYS)}");
                }

                link.Validate();
                return link;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to convert JSON to Link: {ex.Message}.\n----\n{token.ToIndentedString()}", ex);
            }
        }
    }
}
