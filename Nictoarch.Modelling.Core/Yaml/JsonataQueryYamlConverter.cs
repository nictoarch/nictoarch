using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jsonata.Net.Native;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace Nictoarch.Modelling.Core.Yaml
{
    public sealed class JsonataQueryYamlConverter : IYamlTypeConverter
    {
        bool IYamlTypeConverter.Accepts(Type type)
        {
            return type == typeof(JsonataQuery);
        }

        object? IYamlTypeConverter.ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
        {
            Scalar queryEvent = parser.Consume<Scalar>();
            string queryText = queryEvent.Value;
            JsonataQuery query = new JsonataQuery(queryText);
            return query;
        }

        void IYamlTypeConverter.WriteYaml(IEmitter emitter, object? value, Type type, ObjectSerializer serializer)
        {
            throw new NotSupportedException();
        }
    }
}
