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
    internal static class NodeDeserializerValidationDeserializationHelper
    {
        public static void Process(ParsingEvent currentEvent, object value)
        {
            if (value == null)
            {
                return;
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
                deserializable.OnDeserialized(currentEvent);
            }
        }
    }
}
