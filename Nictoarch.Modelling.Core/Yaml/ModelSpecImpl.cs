using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Jsonata.Net.Native;
using Jsonata.Net.Native.Json;
using Nictoarch.Modelling.Core.Elements;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace Nictoarch.Modelling.Core.Yaml
{
    public sealed class ModelSpecImpl
    {
        [Required] public string name { get; set; } = default!;
        [Required] public List<DataPart> data { get; set; } = default!;


        public sealed class DataPart
        {
            [Required] public SourceBase source { get; set; } = default!;
            [Required] public List<Element> elements { get; set; } = default!;
        }

        public abstract class SourceBase
        {
            [Required] public string type { get; set; } = default!;
        }

        public sealed class Element
        {
            [Required] public object extract { get; set; } = default!;
            public JsonataQuery? filter { get; set; }
            public EntitiesSelectorBase? entities { get; set; }
            public JsonataQuery? invalid { get; set; }
        }

        public abstract class EntitiesSelectorBase
        {
            public abstract List<Entity> GetEntities(JToken extractedData);
        }

        public sealed class EntitiesSelectorSingleQuery: EntitiesSelectorBase
        {
            private readonly JsonataQuery m_query;

            internal EntitiesSelectorSingleQuery(JsonataQuery query)
            {
                this.m_query = query;
            }

            public override List<Entity> GetEntities(JToken extractedData)
            {
                this.m_query.Eval(extractedData);
                throw new NotImplementedException("Todo");
            }
        }

        public sealed class EntitesSelectorQueryPerField: EntitiesSelectorBase
        {
            [Required] public JsonataQuery type { get; set; } = default!;
            [Required] public JsonataQuery semantic_id { get; set; } = default!;
            public JsonataQuery? domain_id { get; set; }
            public JsonataQuery? display_name { get; set; }

            public override List<Entity> GetEntities(JToken extractedData)
            {
                throw new NotImplementedException();
            }
        }

        /*
        [Required] public List<ModelProviderSpec> providers { get; set; } = default!;

        public sealed class ModelProviderSpec : IYamlConvertible
        {
            private readonly ModelProviderRegistry m_registry;
            private ModelProviderRegistry.ModelProviderFactory? m_providerFactory;
            private object? m_config;
            private IReadOnlyList<object>? m_entityConfigs;
            private IReadOnlyList<object>? m_validationConfigs;

            public ModelProviderSpec(ModelProviderRegistry registry)
            {
                this.m_registry = registry;
            }

            void IYamlConvertible.Read(IParser parser, Type expectedType, ObjectDeserializer nestedObjectDeserializer)
            {
                parser.Consume<MappingStart>();

                //consume "provider:" key
                {
                    Scalar providerKey = parser.Consume<Scalar>();
                    if (!providerKey.IsKey || providerKey.Value != "provider")
                    {
                        throw new YamlException(providerKey.Start, providerKey.End, "Missing 'provider' key");
                    }
                }
                Scalar providerValue = parser.Consume<Scalar>();
                string providerName = providerValue.Value;
                if (!this.m_registry.GetProviderFactory(providerName, out m_providerFactory))
                {
                    throw new YamlException(providerValue.Start, providerValue.End, $"Unknown Model provider '{providerName}'. Known providers are: {string.Join(", ", this.m_registry.ProviderNames.OrderBy(n => n))}");
                }

                while (parser.TryConsume<Scalar>(out Scalar? key))
                {
                    if (!key.IsKey)
                    {
                        throw new YamlException(key.Start, key.End, "Wtf, should be key: " + key.Value);
                    }

                    ParsingEvent currentEvent = parser.Current!;
                    try
                    {

                        switch (key.Value)
                        {
                        case "config":
                            this.m_config = nestedObjectDeserializer.Invoke(this.m_providerFactory.ConfigType)!;
                            break;
                        case "entities":
                            this.m_entityConfigs = (IReadOnlyList<object>)nestedObjectDeserializer.Invoke(typeof(List<>).MakeGenericType(this.m_providerFactory.EntityConfigType))!;
                            break;
                        case "invalid":
                            this.m_validationConfigs = (IReadOnlyList<object>)nestedObjectDeserializer.Invoke(typeof(List<>).MakeGenericType(this.m_providerFactory.ValidationConfigType))!;
                            break;
                        default:
                            throw new Exception($"Unexpected key '{key.Value}'. Known keys are 'config', 'entities', 'invalid'");
                        }
                    }
                    catch (Exception ex)
                    {
                        throw new YamlException(currentEvent.Start, currentEvent.End, $"Error parsing '{key.Value}': {ex.Message}", ex);
                    }

                }

                if (this.m_config == null)
                {
                    throw new YamlException(providerValue.Start, providerValue.End, "No 'config' specified");
                }

                parser.Consume<MappingEnd>();
            }

            void IYamlConvertible.Write(IEmitter emitter, ObjectSerializer nestedObjectSerializer)
            {
                throw new NotImplementedException();
            }

            internal async Task ProcessAsync(List<Entity> entities, List<Link> links, List<object> invalidObjects, CancellationToken cancellationToken)
            {
                if (this.m_providerFactory == null)
                {
                    throw new Exception("Should not happen! was it not de-serialzied?");
                }
           
                using (IModelProvider provider = await this.m_providerFactory.GetProviderAsync(this.m_config!, cancellationToken))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (this.m_entityConfigs != null)
                    {
                        foreach (object config in this.m_entityConfigs)
                        {
                            List<Entity> newEntities = await this.m_providerFactory.GetEntitiesAsync(provider, config, cancellationToken);
                            entities.AddRange(newEntities);
                            cancellationToken.ThrowIfCancellationRequested();
                        }
                    }

                    if (this.m_validationConfigs != null)
                    {
                        foreach (object config in this.m_validationConfigs)
                        {
                            List<object> newInvalids = await this.m_providerFactory.GetInvalidObjactsAsync(provider, config, cancellationToken);
                            invalidObjects.AddRange(newInvalids);
                            cancellationToken.ThrowIfCancellationRequested();
                        }
                    }
                }
            }
        }
        */
    }
}
