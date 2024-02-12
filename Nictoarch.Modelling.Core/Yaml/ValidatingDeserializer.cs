using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
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
    public sealed class ValidatingDeserializer : INodeDeserializer
    {
        private readonly INodeDeserializer m_nodeDeserializer;

        public ValidatingDeserializer(INodeDeserializer internalDeserialzier)
        {
            this.m_nodeDeserializer = internalDeserialzier;
        }

        public bool Deserialize(IParser parser, Type expectedType, Func<IParser, Type, object?> nestedObjectDeserializer, out object? value)
        {
            ParsingEvent? currentEvent = parser.Current;
            if (!this.m_nodeDeserializer.Deserialize(parser, expectedType, nestedObjectDeserializer, out value)
                || value == null
            )
            {
                return false;
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
}
