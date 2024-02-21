using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.JavaScript;
using System.Text;
using System.Threading.Tasks;
using Jsonata.Net.Native.Json;

namespace Nictoarch.Modelling.Core.Elements
{
    public interface ILinkKey
    {
        public static readonly IEqualityComparer<ILinkKey> Comparer = new ComparerImpl();

        public string type { get; set; }
        public string id { get; set; }

        private sealed class ComparerImpl : IEqualityComparer<ILinkKey>
        {
            bool IEqualityComparer<ILinkKey>.Equals(ILinkKey? x, ILinkKey? y)
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

            int IEqualityComparer<ILinkKey>.GetHashCode(ILinkKey obj)
            {
                int result = 17;
                result = result * 31 + obj.id.GetHashCode();
                result = result * 31 + obj.id.GetHashCode();
                return result;
            }
        }
    }

    public static class LinkKeyExtensions
    {
        public static void Validate(this ILinkKey key)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(key.type, nameof(key.type));
            ArgumentException.ThrowIfNullOrWhiteSpace(key.id, nameof(key.id));
        }

        public static string GetKeyString(this ILinkKey key)
        {
            return JToken.FromObject(key).ToFlatString();
        }
    }

    public sealed class Link: ILinkKey
    {
        public string type { get; set; } = default!;
        public string id { get; set; } = default!;
        public string? display_name { get; set; }

        public EntityKey from { get; set; } = default!;
        public EntityKey to { get; set; } = default!;

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
