using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

        public Entity(string type, string domainId, string semanticId, string displayName)
        {
            this.type = type ?? throw new ArgumentNullException(nameof(type));
            this.domain_id = domainId ?? throw new ArgumentNullException(nameof(domainId));
            this.semantic_id = semanticId ?? throw new ArgumentNullException(nameof(semanticId));
            this.display_name = displayName ?? throw new ArgumentNullException(nameof(displayName));
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
