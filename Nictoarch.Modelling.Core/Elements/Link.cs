using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.JavaScript;
using System.Text;
using System.Threading.Tasks;
using Jsonata.Net.Native.Json;

namespace Nictoarch.Modelling.Core.Elements
{
    public sealed class Link
    {
        public Entity from { get; }
        public Entity to { get; }
        public string domain_id { get; }
        public string semantic_id { get; }
        public string display_text { get; }

        public Link(Entity from, Entity to, string domainId, string semanticId, string displayText)
        {
            this.from = from ?? throw new ArgumentNullException(nameof(from));
            this.to = to ?? throw new ArgumentNullException(nameof(to));
            this.domain_id = domainId ?? throw new ArgumentNullException(nameof(domainId));
            this.semantic_id = semanticId ?? throw new ArgumentNullException(nameof(semanticId));
            this.display_text = displayText ?? throw new ArgumentNullException(nameof(displayText));
        }

        public JObject ToJson()
        {
            JObject result = new JObject();
            result.Add("from_id", new JValue(this.from.semantic_id));
            result.Add("to_id", new JValue(this.to.semantic_id));
            result.Add(nameof(this.domain_id), new JValue(this.domain_id));
            result.Add(nameof(this.semantic_id), new JValue(this.semantic_id));
            result.Add(nameof(this.display_text), new JValue(this.display_text));
            return result;
        }

        public override string ToString()
        {
            return this.ToJson().ToIndentedString();
        }
    }
}
