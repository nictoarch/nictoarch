using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Core;
using YamlDotNet.Serialization;

namespace Nictoarch.Modelling.Core.Yaml
{
    internal sealed class AutoPropertyDeserializer : INodeDeserializer
    {
        private readonly INodeDeserializer m_nodeDeserializer;
        private readonly ModelSpecObjectFactory m_objectFactory;

        public AutoPropertyDeserializer(INodeDeserializer internalDeserialzier, ModelSpecObjectFactory objectFactory)
        {
            this.m_nodeDeserializer = internalDeserialzier;
            this.m_objectFactory = objectFactory;
        }

        bool INodeDeserializer.Deserialize(IParser parser, Type expectedType, Func<IParser, Type, object?> nestedObjectDeserializer, out object? value, ObjectDeserializer rootDeserializer)
        {
            YamlDotNet.Core.Events.ParsingEvent? currentEvent = parser.Current;

            if (!this.m_nodeDeserializer.Deserialize(parser, expectedType, nestedObjectDeserializer, out value, rootDeserializer))
            {
                return false;
            }

            if (value == null)
            {
                return true;
            }

            foreach (PropertyInfo prop in value.GetType().GetProperties().Where(p => p.CanWrite && p.PropertyType == typeof(BasePathAutoProperty)))
            {
                if (prop.GetValue(value) == null)
                {
                    prop.SetValue(value, this.m_objectFactory.Create(prop.PropertyType));
                }
            }

            NodeDeserializerValidationDeserializationHelper.Process(currentEvent!, value);

            return true;
        }
    }
}
