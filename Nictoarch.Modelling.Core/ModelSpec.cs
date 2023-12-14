using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Jsonata.Net.Native.Json;
using Jsonata.Net.Native;
using Nictoarch.Modelling.Core.Elements;
using Nictoarch.Modelling.Core.Yaml;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NodeDeserializers;
using YamlDotNet.Serialization.BufferedDeserialization.TypeDiscriminators;

namespace Nictoarch.Modelling.Core
{
    public sealed class ModelSpec
    {
        private readonly ModelSpecImpl m_spec;
        private readonly SourceRegistry m_registry;

        public string Name => this.m_spec.name;

        private ModelSpec(ModelSpecImpl spec, SourceRegistry registry)
        {
            this.m_spec = spec;
            this.m_registry = registry;
        }

        public static ModelSpec LoadFromFile(string fileName, SourceRegistry registry)
        {
            using (StreamReader reader = new StreamReader(fileName))
            {
                return Load(reader, registry);
            }
        }

        public static ModelSpec Load(TextReader reader, SourceRegistry registry)
        {
            ModelSpecObjectFactory modelSpecObjectFactory = new ModelSpecObjectFactory(registry);

            DeserializerBuilder builder = new DeserializerBuilder()
                .WithObjectFactory(modelSpecObjectFactory)

                //see https://github.com/aaubry/YamlDotNet/wiki/Serialization.Deserializer#withnodedeserializer
                .WithNodeDeserializer(
                    nodeDeserializerFactory: innerDeserialzier => new ValidatingDeserializer(innerDeserialzier), 
                    where: syntax => syntax.InsteadOf<ObjectNodeDeserializer>()
                )
                .WithTypeConverter(new JsonataQueryYamlConverter())

                //see https://github.com/aaubry/YamlDotNet/wiki/Deserialization---Type-Discriminators#determining-type-based-on-the-value-of-a-key
                .WithTypeDiscriminatingNodeDeserializer(options => {
                    
                    options.AddTypeDiscriminator(modelSpecObjectFactory.Discriminator);

                    foreach (ITypeDiscriminator discriminator in registry.GetYamlTypeDiscriminators())
                    {
                        options.AddTypeDiscriminator(discriminator);
                    }
                });

            IDeserializer deserializer = builder.Build();

            ModelSpecImpl spec;
            try
            {
                spec = deserializer.Deserialize<ModelSpecImpl>(reader);
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to deserialzie model spec file >> " + ex.JoinInnerMessages(" >> "), ex);
            }

            return new ModelSpec(spec, registry);
        }

        public async Task<Model> GetModelAsync(CancellationToken cancellationToken = default)
        {
            List<Entity> entities = new List<Entity>();
            List<Link> links = new List<Link>();
            List<JToken> invalidObjects = new List<JToken>();

            foreach (ModelPart part in this.m_spec.data)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!this.m_registry.GetProviderFactory(part.source.type, out SourceRegistry.SourceFactoryWrapper? factory))
                {
                    throw new Exception("Should not happen");
                }
                await using (ISource source = await factory.GetSource(part.source, cancellationToken))
                {
                    foreach (Element element in part.elements)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        JToken data = await factory.Extract(source, element.extract, cancellationToken);
                        
                        cancellationToken.ThrowIfCancellationRequested();

                        if (element.filter != null)
                        {
                            data = element.filter.Eval(data);
                        }

                        cancellationToken.ThrowIfCancellationRequested();

                        if (element.entities != null)
                        {
                            List<Entity> newEntities = element.entities.GetEntities(data);
                            entities.AddRange(newEntities);
                        }

                        cancellationToken.ThrowIfCancellationRequested();

                        if (element.invalid != null)
                        {
                            JToken newInvalids = element.invalid.Eval(data);
                            switch (newInvalids.Type) 
                            {
                            case JTokenType.Undefined:
                            case JTokenType.Null:
                                break;
                            case JTokenType.Array:
                                invalidObjects.AddRange(((JArray)newInvalids).ChildrenTokens);
                                break;
                            case JTokenType.Object:
                            case JTokenType.String:
                                invalidObjects.Add(newInvalids);
                                break;
                            default:
                                throw new Exception("Unexpected token type for Invalid: " + newInvalids.Type);
                            }
                        }
                    }
                }
            }
                 
            return new Model(this.m_spec.name, entities, links, invalidObjects);
        }

        #region YAML classes
        public sealed class ModelSpecImpl
        {
            [Required] public string name { get; set; } = default!;
            [Required] public List<ModelPart> data { get; set; } = default!;
        }

        public sealed class ModelPart
        {
            [Required] public SourceConfigBase source { get; set; } = default!;
            [Required] public List<Element> elements { get; set; } = default!;
        }

        public abstract class SourceConfigBase
        {
            [Required] public string type { get; set; } = default!;
        }

        public sealed class Element
        {
            [Required] public ExtractConfigBase extract { get; set; } = default!;
            public JsonataQuery? filter { get; set; }
            public EntitiesSelectorBase? entities { get; set; }
            public JsonataQuery? invalid { get; set; }
        }

        public abstract class ExtractConfigBase
        {

        }

        public abstract class EntitiesSelectorBase
        {
            public abstract List<Entity> GetEntities(JToken extractedData);

            public static EntitiesSelectorBase Parse(string v)
            {
                return new EntitiesSelectorSingleQuery(new JsonataQuery(v));
            }
        }

        public sealed class EntitiesSelectorSingleQuery : EntitiesSelectorBase
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

        public sealed class EntitesSelectorQueryPerField : EntitiesSelectorBase
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
        #endregion

    }
}
