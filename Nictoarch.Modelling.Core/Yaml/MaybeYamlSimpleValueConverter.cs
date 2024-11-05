using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Jsonata.Net.Native;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace Nictoarch.Modelling.Core.Yaml
{
    public sealed class MaybeYamlSimpleValueConverter : IYamlTypeConverter
    {
        private readonly IObjectFactory m_objectFactory;

        internal MaybeYamlSimpleValueConverter(IObjectFactory objectFactory)
        {
            this.m_objectFactory = objectFactory;
        }

        bool IYamlTypeConverter.Accepts(Type type)
        {
            return typeof(IMaybeYamlSimpleValue).IsAssignableFrom(type);
        }

        object? IYamlTypeConverter.ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
        {
            if (parser.Current is YamlDotNet.Core.Events.Scalar)
            {
                object value = this.m_objectFactory.Create(type);
                List<PropertyInfo> valueProperties = type.GetProperties().Where(pi => pi.IsDefined(typeof(YamlSimpleValueAttribute))).ToList();
                if (valueProperties.Count > 1)
                {
                    throw new YamlException(parser.Current.Start, parser.Current.End, $"Type {type.Name} implements {nameof(IMaybeYamlSimpleValue)} but defines more than one property with {nameof(YamlSimpleValueAttribute)}");
                }
                else if (valueProperties.Count == 0)
                {
                    throw new YamlException(parser.Current.Start, parser.Current.End, $"Type {type.Name} implements {nameof(IMaybeYamlSimpleValue)} but defines no properties with {nameof(YamlSimpleValueAttribute)}");
                }

                PropertyInfo pi = valueProperties[0];
                if (pi.PropertyType != typeof(string))
                {
                    throw new YamlException(parser.Current.Start, parser.Current.End, $"Type {type.Name} implements {nameof(IMaybeYamlSimpleValue)} but it's SiompleValue property {pi.Name} is of type {pi.PropertyType.Name} while it should be String");
                }
                if (!pi.CanWrite)
                {
                    throw new YamlException(parser.Current.Start, parser.Current.End, $"Type {type.Name} implements {nameof(IMaybeYamlSimpleValue)} but it's SiompleValue property {pi.Name} is ReadOnly");
                }

                Scalar scalar = parser.Consume<Scalar>();

                pi.SetValue(value, scalar.Value);

                return value;
            }
            else
            {
                return rootDeserializer.Invoke(type);
            }
        }

        void IYamlTypeConverter.WriteYaml(IEmitter emitter, object? value, Type type, ObjectSerializer serializer)
        {
            throw new NotSupportedException();
        }
    }
}
