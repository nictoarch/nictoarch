using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Jsonata.Net.Native.Json;


namespace Nictoarch.Modelling.Core.Elements
{
    public class EntityKey
    {
        public static readonly IEqualityComparer<EntityKey> Comparer = new ComparerImpl();

        public string type { get; set; } = default!;
        public string? group { get; set; }
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

        private sealed class ComparerImpl : IEqualityComparer<EntityKey>
        {
            bool IEqualityComparer<EntityKey>.Equals(EntityKey? x, EntityKey? y)
            {
                if (x == null && y == null)
                {
                    return true;
                }
                if (x == null || y == null ) 
                { 
                    return false; 
                }

                return x.type == y.type
                    && x.group == y.group
                    && x.id == y.id;
            }

            int IEqualityComparer<EntityKey>.GetHashCode(EntityKey obj)
            {
                int result = 17;
                result = result * 31 + obj.id.GetHashCode();
                result = result * 31 + obj.group?.GetHashCode() ?? 0;
                result = result * 31 + obj.id.GetHashCode();
                return result;
            }
        }
    }

    public sealed class Entity: EntityKey
    {
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
