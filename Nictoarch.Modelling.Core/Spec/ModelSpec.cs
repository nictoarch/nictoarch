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

namespace Nictoarch.Modelling.Core.Spec
{
    public sealed class ModelSpec
    {
        private readonly ModelSpecImpl m_spec;
        private readonly SourceRegistry m_registry;

        public event Action<string>? OnTrace;

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
                return Load(reader, registry, basePath: Path.GetDirectoryName(fileName));
            }
        }

        public static ModelSpec Load(TextReader reader, SourceRegistry registry, string? basePath = null)
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

                .WithTagMapping(YamlnplaceNodeDeserializer.TAG, typeof(object)) // tag needs to be registered so that validation passes
                .WithNodeDeserializer(
                    new YamlnplaceNodeDeserializer(basePath ?? Directory.GetCurrentDirectory()),
                    where: syntax => syntax.OnTop()
                )
                .WithTagMapping(YamEnvNodeDeserializer.TAG, typeof(string))     // tag needs to be registered so that validation passes
                .WithNodeDeserializer(
                    new YamEnvNodeDeserializer(),
                    where: syntax => syntax.OnTop()
                )

                //see https://github.com/aaubry/YamlDotNet/wiki/Deserialization---Type-Discriminators#determining-type-based-on-the-value-of-a-key
                .WithTypeDiscriminatingNodeDeserializer(options => {

                    options.AddTypeDiscriminator(modelSpecObjectFactory.Discriminator);
                    options.AddTypeDiscriminator(new EntitiesSelectorTypeDiscriminator());

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
            List<object> invalidObjects = new List<object>();

            this.OnTrace?.Invoke("Getting model " + this.Name);

            foreach (ModelPart part in this.m_spec.data)
            {
                this.OnTrace?.Invoke($"Processing model part ({part.source.type})");
                cancellationToken.ThrowIfCancellationRequested();

                if (!this.m_registry.GetProviderFactory(part.source.type, out SourceRegistry.SourceFactoryWrapper? factory))
                {
                    throw new Exception("Should not happen");
                }
                await using (ISource source = await factory.GetSource(part.source, cancellationToken))
                {
                    this.OnTrace?.Invoke($"Got source");
                    foreach (Element element in part.elements)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        JToken data = await factory.Extract(source, element.extract, cancellationToken);
                        this.OnTrace?.Invoke($"Extracted element data:\n" + data.ToIndentedString());

                        cancellationToken.ThrowIfCancellationRequested();

                        if (element.filter != null)
                        {
                            data = element.filter.Eval(data, "filter");
                            this.OnTrace?.Invoke($"Filtered element data:\n" + data.ToIndentedString());
                        }

                        cancellationToken.ThrowIfCancellationRequested();

                        if (element.entities != null)
                        {
                            try
                            {
                                List<Entity> newEntities = element.entities.GetEntities(data);
                                entities.AddRange(newEntities);
                                this.OnTrace?.Invoke($"Got entities:\n" + JToken.FromObject(newEntities).ToIndentedString());
                            }
                            catch (Exception ex)
                            {
                                throw new Exception("Failed to get Entities from the element: " + ex.Message, ex);
                            }
                        }
                        else
                        {
                            this.OnTrace?.Invoke($"No '{nameof(element.entities)}' section");
                        }

                        cancellationToken.ThrowIfCancellationRequested();

                        if (element.links != null)
                        {
                            try
                            {
                                List<Link> newLinks = element.links.GetLinks(data);
                                links.AddRange(newLinks);
                                this.OnTrace?.Invoke($"Got links:\n" + JToken.FromObject(newLinks).ToIndentedString());
                            }
                            catch (Exception ex)
                            {
                                throw new Exception("Failed to get Links from the element: " + ex.Message, ex);
                            }
                        }
                        else
                        {
                            this.OnTrace?.Invoke($"No '{nameof(element.links)}' section");
                        }

                        cancellationToken.ThrowIfCancellationRequested();

                        if (element.invalid != null)
                        {
                            JToken newInvalids = element.invalid.query.Eval(data, "invalid");
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
                            this.OnTrace?.Invoke($"Got invalids:\n" + newInvalids.ToIndentedString());
                        }
                        else
                        {
                            this.OnTrace?.Invoke($"No '{nameof(element.invalid)}' section");
                        }
                    }
                }
            }

            return new Model(this.m_spec.name, entities, links, invalidObjects);
        }
    }
}
