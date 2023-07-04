using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.ObjectFactories;

namespace Nictoarch.Modelling.Core.Yaml
{
    public sealed class ModelRegistryObjectFactory : IObjectFactory
    {
        private readonly IObjectFactory m_fallback;
        private readonly ModelProviderRegistry m_registry;

        public ModelRegistryObjectFactory(ModelProviderRegistry registry)
        {
            m_fallback = new DefaultObjectFactory();
            m_registry = registry;
        }

        object IObjectFactory.Create(Type type)
        {
            if (type == typeof(ModelSpecImpl.EntitySelector))
            {
                return new ModelSpecImpl.EntitySelector(m_registry);
            }
            else
            {
                return m_fallback.Create(type);
            }
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
    }
}
