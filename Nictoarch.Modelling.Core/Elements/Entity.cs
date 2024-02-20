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
        public string? group { get; set; }
        public string id { get; set; } = default!;
        public string? display_name { get; set; }
        public Dictionary<string, object>? properties { get; set; } = null;
        public Dictionary<string, object>? properties_info { get; set; } = null;

        public string GetIdentityKey() => $"{this.type}|{this.group}|{this.id}";

        public void Validate()
        {
            ArgumentException.ThrowIfNullOrEmpty(this.type, nameof(this.type));
            ArgumentException.ThrowIfNullOrEmpty(this.id, nameof(this.id));
        }

        public JObject ToJson()
        {
            /*
            JObject result = new JObject();
            result.Add(nameof(this.type), new JValue(this.type));
            result.Add(nameof(this.id), new JValue(this.id));
            if (this.group != null)
            {
                result.Add(nameof(this.group), new JValue(this.group));
            }
            if (this.display_name != null)
            {
                result.Add(nameof(this.display_name), new JValue(this.display_name));
            }
            if (this.properties != null)
            {
                result.Add(nameof(this.properties), JToken.FromObject(this.properties));
            }
            if (this.properties_info != null)
            {
                result.Add(nameof(this.properties_info), JToken.FromObject(this.properties_info));
            }
            return result;
            */
            JObject result = (JObject)JObject.FromObject(this);
            return result;
        }

        public override string ToString()
        {
            return this.ToJson().ToIndentedString(Jsonata.Constants.NO_NULLS);
        }
    }
}
