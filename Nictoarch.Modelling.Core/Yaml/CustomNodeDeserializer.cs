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
        private readonly INodeDeserializer m_nodeDeserializer;
        private readonly ModelSpecObjectFactory m_objectFactory;

        public CustomNodeDeserializer(INodeDeserializer internalDeserialzier, ModelSpecObjectFactory objectFactory)
        {
            this.m_nodeDeserializer = internalDeserialzier;
            this.m_objectFactory = objectFactory;
        }

        public virtual bool Deserialize(IParser parser, Type expectedType, Func<IParser, Type, object?> nestedObjectDeserializer, out object? value, ObjectDeserializer rootDeserializer)
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

            return true;
        }

    }

    internal sealed class CustomObjectNodeDeserializer : CustomNodeDeserializer
    {
        public CustomObjectNodeDeserializer(INodeDeserializer internalDeserialzier, ModelSpecObjectFactory objectFactory) 
            : base(internalDeserialzier, objectFactory)
        {
        }
    }

    internal sealed class CustomScalarNodeDeserializer : CustomNodeDeserializer
    {
        public CustomScalarNodeDeserializer(INodeDeserializer internalDeserialzier, ModelSpecObjectFactory objectFactory)
            : base(internalDeserialzier, objectFactory)
        {
        }

        public override bool Deserialize(IParser parser, Type expectedType, Func<IParser, Type, object?> nestedObjectDeserializer, out object? value, ObjectDeserializer rootDeserializer)
        {
            if (parser.Current is not YamlDotNet.Core.Events.Scalar)
            {
                value = null;
                return false;
            }

            return base.Deserialize(parser, expectedType, nestedObjectDeserializer, out value, rootDeserializer);
        }
    }
}
