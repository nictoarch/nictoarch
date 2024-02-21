using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Jsonata.Net.Native.Json;


namespace Nictoarch.Modelling.Core.Elements
{
    public interface IEntityKey
    {
        public string type { get; set; }
        public string? group { get; set; }
        public string id { get; set; }

        public static readonly IEqualityComparer<IEntityKey> Comparer = new ComparerImpl();

        private sealed class ComparerImpl : IEqualityComparer<IEntityKey>
        {
            bool IEqualityComparer<IEntityKey>.Equals(IEntityKey? x, IEntityKey? y)
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
                    && x.group == y.group
                    && x.id == y.id;
            }

            int IEqualityComparer<IEntityKey>.GetHashCode(IEntityKey obj)
            {
                int result = 17;
                result = result * 31 + obj.id.GetHashCode();
                result = result * 31 + obj.group?.GetHashCode() ?? 0;
                result = result * 31 + obj.id.GetHashCode();
                return result;
            }
        }
    }

    public static class EntityKeyExtensions
    {
        public static void Validate(this IEntityKey key)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(key.type, nameof(key.type));
            ArgumentException.ThrowIfNullOrWhiteSpace(key.id, nameof(key.id));
        }

        public static string GetKeyString(this IEntityKey key)
        {
            return JToken.FromObject(key).ToFlatString();
        }
    }

    public sealed class EntityKey: IEntityKey
    {
        public string type { get; set; } = default!;
        public string? group { get; set; }
        public string id { get; set; } = default!;

        public override string ToString()
        {
            return this.GetKeyString();
        }
    }

    public sealed class Entity: IEntityKey
    {
        public string type { get; set; } = default!;
        public string? group { get; set; }
        public string id { get; set; } = default!;

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
