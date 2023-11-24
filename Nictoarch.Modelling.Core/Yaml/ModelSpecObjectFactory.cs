using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.BufferedDeserialization.TypeDiscriminators;
using YamlDotNet.Serialization.ObjectFactories;

namespace Nictoarch.Modelling.Core.Yaml
{
    //see https://github.com/aaubry/YamlDotNet/wiki/Serialization.Deserializer#withobjectfactoryiobjectfactory
    public sealed class ModelSpecObjectFactory : IObjectFactory
    {
        private readonly IObjectFactory m_fallback;
        private readonly SourceRegistry m_registry;

        private SourceRegistry.SourceFactoryWrapper? m_currentFactory = null;
        internal ITypeDiscriminator Discriminator { get; }

        internal SourceRegistry.SourceFactoryWrapper CurrentFactory
        {
            get
            {
                if (this.m_currentFactory == null)
                {
                    throw new Exception("Should not happen - no current factory");
                }
                else
                {
                    return this.m_currentFactory;
                }
            }
        }

        public ModelSpecObjectFactory(SourceRegistry registry)
        {
            this.m_fallback = new DefaultObjectFactory();
            this.m_registry = registry;
            this.Discriminator = new TypeDiscriminator(this);
        }

        object IObjectFactory.Create(Type type)
        {
            object result = this.m_fallback.Create(type);

            if (typeof(ModelSpec.SourceConfigBase).IsAssignableFrom(type))
            {
                if (!this.m_registry.GetProviderByConfigType(type, out SourceRegistry.SourceFactoryWrapper? factory))
                {
                    throw new Exception($"Should not happen: no factory with source config type {type.Name} ({type.FullName})");
                }
                else
                {
                    this.m_currentFactory = factory;
                }
            }

            return result;
        }

        object? IObjectFactory.CreatePrimitive(Type type)
        {
            return m_fallback.CreatePrimitive(type);
        }

        bool IObjectFactory.GetDictionary(IObjectDescriptor descriptor, out IDictionary? dictionary, out Type[]? genericArguments)
        {
            return m_fallback.GetDictionary(descriptor, out dictionary, out genericArguments);
        }

        Type IObjectFactory.GetValueType(Type type)
        {
            return m_fallback.GetValueType(type);
        }

        private sealed class TypeDiscriminator : ITypeDiscriminator
        {
            private readonly ModelSpecObjectFactory m_parent;

            public TypeDiscriminator(ModelSpecObjectFactory factory)
            {
                this.m_parent = factory;
            }

            Type ITypeDiscriminator.BaseType => typeof(ModelSpec.ExtractConfigBase);

            bool ITypeDiscriminator.TryDiscriminate(IParser buffer, out Type? suggestedType)
            {
                suggestedType = this.m_parent.CurrentFactory.ExtractType;
                return true;
            }
        }
    }
}
