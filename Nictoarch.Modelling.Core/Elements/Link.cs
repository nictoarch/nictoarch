using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.JavaScript;
using System.Text;
using System.Threading.Tasks;
using Jsonata.Net.Native.Json;

namespace Nictoarch.Modelling.Core.Elements
{
    public class LinkKey
    {
        public static readonly IEqualityComparer<LinkKey> Comparer = new ComparerImpl();

        public string type { get; set; } = default!;
        public string id { get; set; } = default!;

        public void Validate()
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(this.type, nameof(this.type));
            ArgumentException.ThrowIfNullOrWhiteSpace(this.id, nameof(this.id));
        }

        public string GetKeyString()
        {
            return JToken.FromObject(this).ToFlatString();
        }

        public override string ToString()
        {
            return this.GetKeyString();
        }

        private sealed class ComparerImpl : IEqualityComparer<LinkKey>
        {
            bool IEqualityComparer<LinkKey>.Equals(LinkKey? x, LinkKey? y)
            {
                if (x == null && y == null)
                {
                    return true;
                }
                if (x == null || y == null)
                {
                    return false;
                }

                return x.type == y.type
                    && x.id == y.id;
            }

            int IEqualityComparer<LinkKey>.GetHashCode(LinkKey obj)
            {
                int result = 17;
                result = result * 31 + obj.id.GetHashCode();
                result = result * 31 + obj.id.GetHashCode();
                return result;
            }
        }
    }

    public sealed class Link: LinkKey
    {
        public EntityKey from { get; set; } = default!;
        public EntityKey to { get; set; } = default!;
        public string? display_name { get; set; }

        public Dictionary<string, object>? properties { get; set; } = null;
        public Dictionary<string, object>? properties_info { get; set; } = null;

        public JObject ToJson()
        {
            JObject result = (JObject)JToken.FromObject(this);
            return result;
        }

        public override string ToString()
        {
            return this.ToJson().ToIndentedString(Jsonata.Constants.NO_NULLS);
        }

    }
}
