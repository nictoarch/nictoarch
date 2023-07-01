using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jsonata.Net.Native.Json;

namespace Nictoarch.Modelling.Core.Elements
{
    public sealed class Model
    {
        public readonly string displayName;

        public readonly IReadOnlyList<Entity> entities;
        public readonly IReadOnlyList<Link> links;

        public Model(string displayName, IReadOnlyList<Entity> entities, IReadOnlyList<Link> links)
        {
            this.displayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
            this.entities = entities ?? throw new ArgumentNullException(nameof(entities));
            this.links = links ?? throw new ArgumentNullException(nameof(links));
        }

        public JObject ToJson()
        {
            JObject result = new JObject();
            result.Add("display_name", new JValue(this.displayName));
            
            JArray entitiesArray = new JArray(this.entities.Count);
            foreach (Entity entity in this.entities)
            {
                entitiesArray.Add(entity.ToJson());
            }

            result.Add("entities", entitiesArray);

            JArray linksArray = new JArray(this.links.Count);
            foreach (Link link in this.links)
            {
                linksArray.Add(link.ToJson());
            }

            result.Add("links", linksArray);

            return result;
        }

        public override string ToString()
        {
            return this.ToJson().ToIndentedString();
        }
    }
}
