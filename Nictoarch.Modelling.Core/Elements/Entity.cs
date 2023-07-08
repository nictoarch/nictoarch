using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Jsonata.Net.Native.Json;

namespace Nictoarch.Modelling.Core.Elements
{
    public sealed class Entity
    {
        public string type { get; }
        public string domain_id { get; }
        public string semantic_id { get; }
        public string display_name { get; }

        [JsonConstructor]
        public Entity(string type, string domain_id, string semantic_id, string display_name)
        {
            this.type = type ?? throw new ArgumentNullException(nameof(type));
            this.domain_id = domain_id ?? throw new ArgumentNullException(nameof(domain_id));
            this.semantic_id = semantic_id ?? throw new ArgumentNullException(nameof(semantic_id));
            this.display_name = display_name ?? throw new ArgumentNullException(nameof(display_name));
        }

        public JObject ToJson()
        {
            return (JObject)JToken.FromObject(this);
        }

        public override string ToString()
        {
            return this.ToJson().ToIndentedString();
        }
    }
}
