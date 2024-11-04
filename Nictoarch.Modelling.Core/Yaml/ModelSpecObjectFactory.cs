using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nictoarch.Modelling.Core.BuiltinSources.Combined;
using Nictoarch.Modelling.Core.Spec;
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
        private readonly string m_basePath;

        private readonly Stack<SourceRegistry.SourceFactoryWrapper> m_factoryStack = new Stack<SourceRegistry.SourceFactoryWrapper>();
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

        public ModelSpecObjectFactory(SourceRegistry registry, string basePath)
        {
            this.m_fallback = new DefaultObjectFactory();
            this.m_registry = registry;
            this.m_basePath = basePath;
            this.Discriminator = new TypeDiscriminator(this);
        }

        //IObjectFactory
        public object Create(Type type)
        {
            if (type == typeof(BasePathAutoProperty))
            {
                return new BasePathAutoProperty(this.m_basePath);
            }

            if (typeof(SourceConfigBase).IsAssignableFrom(type))
            {
                if (!this.m_registry.GetProviderByConfigType(type, out SourceRegistry.SourceFactoryWrapper? factory))
                {
                    throw new Exception($"Should not happen: no factory with source config type {type.Name} ({type.FullName})");
                }

                this.m_currentFactory = factory;
                if (type == typeof(CombinedSourceConfig))
                {
                    //special hack for nested providers
                    this.m_factoryStack.Push(this.m_currentFactory);
                }
            }
            
            object result = this.m_fallback.Create(type);
            return result;
        }

        object? IObjectFactory.CreatePrimitive(Type type)
        {
            return this.m_fallback.CreatePrimitive(type);
        }

        bool IObjectFactory.GetDictionary(IObjectDescriptor descriptor, out IDictionary? dictionary, out Type[]? genericArguments)
        {
            return this.m_fallback.GetDictionary(descriptor, out dictionary, out genericArguments);
        }

        Type IObjectFactory.GetValueType(Type type)
        {
            return this.m_fallback.GetValueType(type);
        }

        void IObjectFactory.ExecuteOnDeserializing(object value)
        {
            this.m_fallback.ExecuteOnDeserializing(value);
        }

        void IObjectFactory.ExecuteOnDeserialized(object value)
        {
            this.m_fallback.ExecuteOnDeserialized(value);

            if (value.GetType() == typeof(CombinedSourceConfig))
            {
                //special hack for nested providers
                this.m_currentFactory = this.m_factoryStack.Pop();
            }
        }

        void IObjectFactory.ExecuteOnSerializing(object value)
        {
            this.m_fallback.ExecuteOnSerializing(value);
        }

        void IObjectFactory.ExecuteOnSerialized(object value)
        {
            this.m_fallback.ExecuteOnSerialized(value);
        }

        private sealed class TypeDiscriminator : ITypeDiscriminator
        {
            private readonly ModelSpecObjectFactory m_parent;

            public TypeDiscriminator(ModelSpecObjectFactory factory)
            {
                this.m_parent = factory;
            }

            Type ITypeDiscriminator.BaseType => typeof(ExtractConfigBase);

            bool ITypeDiscriminator.TryDiscriminate(IParser buffer, out Type? suggestedType)
            {
                suggestedType = this.m_parent.CurrentFactory.ExtractType;
                return true;
            }
        }
    }
}
