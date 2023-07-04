using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Nictoarch.Modelling.Core.Elements;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace Nictoarch.Modelling.Core.Yaml
{
    internal sealed class ModelSpecImpl
    {
        public string name { get; set; } = default!;
        public List<EntitySelector> entities { get; set; } = default!;

        public sealed class EntitySelector : IYamlConvertible
        {
            private readonly ModelProviderRegistry m_registry;
            private ModelProviderRegistry.EntityProvider? m_provider;
            private object? m_specObj;

            public EntitySelector(ModelProviderRegistry registry)
            {
                this.m_registry = registry;
            }

            public Task<List<Entity>> GetEntitiesAsync(CancellationToken cancellationToken)
            {
                if (this.m_provider == null || this.m_specObj == null)
                {
                    throw new Exception("Should not happen! was it not de-serialzied?");
                }
                return m_provider.GetEntitiesAsync(this.m_specObj, cancellationToken);
            }

            void IYamlConvertible.Read(IParser parser, Type expectedType, ObjectDeserializer nestedObjectDeserializer)
            {
                parser.Consume<MappingStart>();

                //consume "provider:" key
                {
                    Scalar providerKey = parser.Consume<Scalar>();
                    if (!providerKey.IsKey || providerKey.Value != "provider")
                    {
                        throw new Exception("Missing 'provider' key");
                    }
                }
                Scalar providerValue = parser.Consume<Scalar>();
                string providerName = providerValue.Value;
                if (!this.m_registry.GetEntityProvider(providerName, out m_provider))
                {
                    throw new Exception($"Unknown entity provider '{providerName}'. Known providers are: {string.Join(", ", this.m_registry.EntityProviderNames.OrderBy(n => n))}");
                }

                //consume "spec:" key
                {
                    Scalar specKey = parser.Consume<Scalar>();
                    if (!specKey.IsKey || specKey.Value != "spec")
                    {
                        throw new Exception("Missing 'spec' key");
                    }
                }

                this.m_specObj = nestedObjectDeserializer.Invoke(this.m_provider.SpecType)!;
                parser.Consume<MappingEnd>();
            }

            void IYamlConvertible.Write(IEmitter emitter, ObjectSerializer nestedObjectSerializer)
            {
                throw new NotImplementedException();
            }
        }
    }
}
