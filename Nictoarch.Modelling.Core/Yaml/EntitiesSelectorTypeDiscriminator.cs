using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Core.Events;
using YamlDotNet.Core;
using YamlDotNet.Serialization.BufferedDeserialization.TypeDiscriminators;
using YamlDotNet.Serialization;
using Nictoarch.Modelling.Core.Spec;

namespace Nictoarch.Modelling.Core.Yaml
{
    //based on https://github.com/aaubry/YamlDotNet/blob/847230593e95750d4294ca72c98a4bd46bdcf265/YamlDotNet/Serialization/BufferedDeserialization/TypeDiscriminators/KeyValueTypeDiscriminator.cs
    internal sealed class EntitiesSelectorTypeDiscriminator : ITypeDiscriminator
    {
        Type ITypeDiscriminator.BaseType => typeof(EntitiesSelectorBase);

        public EntitiesSelectorTypeDiscriminator()
        {
        }

        /// <summary>
        /// Checks if the current parser contains the target key, and that it's value matches one of the type mappings.
        /// If so, return true, and the matching type.
        /// Otherwise, return false.
        /// This will consume the parser, so you will usually need the parser to be a buffer so an instance 
        /// of the discriminated type can be deserialized later.
        /// </summary>
        /// <param name="parser">The IParser to consume and discriminate a type from.</param>
        /// <param name="suggestedType">The output type discriminated. Null if there target key was not present of if the value
        /// of the target key was not within the type mapping.</param>
        /// <returns>Returns true if the discriminator matched the yaml stream.</returns>
        bool ITypeDiscriminator.TryDiscriminate(IParser parser, out Type? suggestedType)
        {
            if (parser.Current is Scalar)
            {
                //actually will not happen, but some type discriminator is needed to return query per field. 
                suggestedType = typeof(EntitiesSelectorSingleQuery);
            }
            else
            {
                suggestedType = typeof(EntitiesSelectorQueryPerField);
            }
            return true;
        }
    }
}
