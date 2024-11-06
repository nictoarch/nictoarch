using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NodeDeserializers;

namespace Nictoarch.Modelling.Core.Yaml
{
    // see https://github.com/aaubry/YamlDotNet/issues/202#issuecomment-830712803
    // see https://github.com/aaubry/YamlDotNet/wiki/Serialization.Deserializer#withnodedeserializer
    internal sealed class MaybeSimpleValueScalarNodeDeserializer : INodeDeserializer
    {
        private readonly INodeDeserializer m_nodeDeserializer;
        private readonly IObjectFactory m_objectFactory;

        public MaybeSimpleValueScalarNodeDeserializer(INodeDeserializer internalDeserialzier, IObjectFactory objectFactory)
        {
            this.m_nodeDeserializer = internalDeserialzier;
            this.m_objectFactory = objectFactory;
        }

        public bool Deserialize(IParser parser, Type expectedType, Func<IParser, Type, object?> nestedObjectDeserializer, out object? value, ObjectDeserializer rootDeserializer)
        {
            if (parser.Current is not YamlDotNet.Core.Events.Scalar)
            {
                value = null;
                return false;
            }

            ParsingEvent? currentEvent = parser.Current;

            if (typeof(IMaybeYamlSimpleValue).IsAssignableFrom(expectedType))
            {
                value = this.m_objectFactory.Create(expectedType);
                List<PropertyInfo> valueProperties = expectedType.GetProperties().Where(pi => pi.IsDefined(typeof(YamlSimpleValueAttribute))).ToList();
                if (valueProperties.Count > 1)
                {
                    throw new YamlException(currentEvent.Start, currentEvent.End, $"Type {expectedType.Name} implements {nameof(IMaybeYamlSimpleValue)} but defines more than one property with {nameof(YamlSimpleValueAttribute)}");
                }
                else if (valueProperties.Count == 0)
                {
                    throw new YamlException(currentEvent.Start, currentEvent.End, $"Type {expectedType.Name} implements {nameof(IMaybeYamlSimpleValue)} but defines no properties with {nameof(YamlSimpleValueAttribute)}");
                }

                PropertyInfo pi = valueProperties[0];
                if (pi.PropertyType != typeof(string))
                {
                    throw new YamlException(currentEvent.Start, currentEvent.End, $"Type {expectedType.Name} implements {nameof(IMaybeYamlSimpleValue)} but it's SiompleValue property {pi.Name} is of type {pi.PropertyType.Name} while it should be String");
                }
                if (!pi.CanWrite)
                {
                    throw new YamlException(currentEvent.Start, currentEvent.End, $"Type {expectedType.Name} implements {nameof(IMaybeYamlSimpleValue)} but it's SiompleValue property {pi.Name} is ReadOnly");
                }

                Scalar scalar = parser.Consume<Scalar>();

                pi.SetValue(value, scalar.Value);
            }
            else if (!this.m_nodeDeserializer.Deserialize(parser, expectedType, nestedObjectDeserializer, out value, rootDeserializer))
            {
                return false;
            }

            if (value == null)
            {
                return true;
            }

            NodeDeserializerValidationDeserializationHelper.Process(currentEvent!, value);
            return true;
        }
    }
}
