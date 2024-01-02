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
        public string type { get; set; } = default!;
        public string domain_id { get; set; } = default!;
        public string semantic_id { get; set; } = default!;
        public string display_name { get; set; } = default!;
        public Dictionary<string, object>? properties { get; set; } = null;

        public void Validate()
        {
            ArgumentException.ThrowIfNullOrEmpty(this.type, nameof(this.type));
            ArgumentException.ThrowIfNullOrEmpty(this.domain_id, nameof(this.domain_id));
            ArgumentException.ThrowIfNullOrEmpty(this.semantic_id, nameof(this.semantic_id));
            ArgumentException.ThrowIfNullOrEmpty(this.display_name, nameof(this.display_name));
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
