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
    internal abstract class CustomNodeDeserializer : INodeDeserializer
    {
        protected readonly INodeDeserializer m_nodeDeserializer;
        protected readonly ModelSpecObjectFactory m_objectFactory;

        protected CustomNodeDeserializer(INodeDeserializer internalDeserialzier, ModelSpecObjectFactory objectFactory)
        {
            this.m_nodeDeserializer = internalDeserialzier;
            this.m_objectFactory = objectFactory;
        }

        protected void PostProcessDeserializedObject(object? value, ParsingEvent? currentEvent)
        {
            if (value == null)
            {
                return;
            }

            foreach (PropertyInfo prop in value.GetType().GetProperties().Where(p => p.CanWrite && p.PropertyType == typeof(BasePathAutoProperty)))
            {
                if (prop.GetValue(value) == null)
                {
                    prop.SetValue(value, this.m_objectFactory.Create(prop.PropertyType));
                }
            }

            ValidationContext context = new ValidationContext(value, null, null);

            try
            {
                Validator.ValidateObject(value, context, true);
            }
            catch (ValidationException e)
            {
                if (currentEvent == null)
                {
                    throw;
                }

                throw new YamlException(currentEvent.Start, currentEvent.End, e.Message, e);
            }

            //see https://github.com/aaubry/YamlDotNet/issues/785
            if (value is IYamlOnDeserialized deserializable)
            {
                deserializable.OnDeserialized(currentEvent!);
            }
        }

        public abstract bool Deserialize(IParser parser, Type expectedType, Func<IParser, Type, object?> nestedObjectDeserializer, out object? value, ObjectDeserializer rootDeserializer);
    }
}
