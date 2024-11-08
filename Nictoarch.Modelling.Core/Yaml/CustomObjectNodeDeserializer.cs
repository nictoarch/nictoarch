using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Core;
using YamlDotNet.Serialization;

namespace Nictoarch.Modelling.Core.Yaml
{
    internal sealed class CustomObjectNodeDeserializer : CustomNodeDeserializer
    {
        public CustomObjectNodeDeserializer(INodeDeserializer internalDeserialzier, ModelSpecObjectFactory objectFactory) 
            : base(internalDeserialzier, objectFactory)
        {
        }

        public override bool Deserialize(IParser parser, Type expectedType, Func<IParser, Type, object?> nestedObjectDeserializer, out object? value, ObjectDeserializer rootDeserializer)
        {
            YamlDotNet.Core.Events.ParsingEvent? currentEvent = parser.Current;

            if (!this.m_nodeDeserializer.Deserialize(parser, expectedType, nestedObjectDeserializer, out value, rootDeserializer))
            {
                return false;
            }

            this.PostProcessDeserializedObject(value, currentEvent);

            return true;
        }
    }
}
