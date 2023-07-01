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
        public string domainId { get; }
        public string semanticId { get; }
        public string displayName { get; }

        public Entity(string type, string domainId, string semanticId, string displayName)
        {
            this.type = type ?? throw new ArgumentNullException(nameof(type));
            this.domainId = domainId ?? throw new ArgumentNullException(nameof(domainId));
            this.semanticId = semanticId ?? throw new ArgumentNullException(nameof(semanticId));
            this.displayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
        }

        public JObject ToJson()
        {
            JObject result = new JObject();
            result.Add("type", new JValue(this.type));
            result.Add("domain_id", new JValue(this.domainId));
            result.Add("semantic_id", new JValue(this.semanticId));
            result.Add("display_name", new JValue(this.displayName));
            return result;
        }

        public override string ToString()
        {
            return this.ToJson().ToIndentedString();
        }
    }
}
