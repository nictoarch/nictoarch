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
        public string domainId { get; }
        public string semanticId { get; }
        public string displayText { get; }

        public Link(Entity from, Entity to, string domainId, string semanticId, string displayText)
        {
            this.from = from ?? throw new ArgumentNullException(nameof(from));
            this.to = to ?? throw new ArgumentNullException(nameof(to));
            this.domainId = domainId ?? throw new ArgumentNullException(nameof(domainId));
            this.semanticId = semanticId ?? throw new ArgumentNullException(nameof(semanticId));
            this.displayText = displayText ?? throw new ArgumentNullException(nameof(displayText));
        }

        public JObject ToJson()
        {
            JObject result = new JObject();
            result.Add("from_id", new JValue(this.from.semantic_id));
            result.Add("to_id", new JValue(this.to.semantic_id));
            result.Add("domain_id", new JValue(this.domainId));
            result.Add("semantic_id", new JValue(this.semanticId));
            result.Add("display_text", new JValue(this.displayText));
            return result;
        }

        public override string ToString()
        {
            return this.ToJson().ToIndentedString();
        }
    }
}
