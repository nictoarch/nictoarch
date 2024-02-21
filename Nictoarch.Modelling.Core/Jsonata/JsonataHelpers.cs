using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jsonata.Net.Native.Json;
using Nictoarch.Modelling.Core.Elements;

namespace Nictoarch.Modelling.Core.Jsonata
{
    internal static class JsonataHelpers
    {
        internal static string GetString(this JObject obj, string name)
        {
            if (!obj.Properties.TryGetValue(name, out JToken? prop)) 
            {
                throw new Exception($"Missing required property '{name}'");
            }

            if (prop.Type != JTokenType.String)
            {
                throw new Exception($"Property '{name}' expected to be a String but it is {prop.Type}");
            }

            return (string)prop;
        }

        internal static string GetString(this JObject obj, string name, string defaultValue)
        {
            if (!obj.Properties.TryGetValue(name, out JToken? prop))
            {
                return defaultValue;
            }

            if (prop.Type != JTokenType.String)
            {
                throw new Exception($"Property '{name}' expected to be a String but it is {prop.Type}");
            }

            return (string)prop;
        }

        internal static string? GetStringNullable(this JObject obj, string name)
        {
            if (!obj.Properties.TryGetValue(name, out JToken? prop))
            {
                return null;
            }

            if (prop.Type != JTokenType.String)
            {
                throw new Exception($"Property '{name}' expected to be a String but it is {prop.Type}");
            }

            return (string)prop;
        }

        internal static EntityKey GetEntityKey(this JObject obj, string name)
        {
            if (!obj.Properties.TryGetValue(name, out JToken? prop))
            {
                throw new Exception($"Missing required property '{name}'");
            }

            if (prop.Type != JTokenType.Object)
            {
                throw new Exception($"Property '{name}' expected to be an Object but it is {prop.Type}");
            }

            try
            {
                EntityKey result = prop.ToObject<EntityKey>(Constants.ALLOW_MISSING);
                result.Validate();
                return result;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error deserializing property '{name}' to EntityKey: {ex.Message}", ex);
            }
        }
    }
}
