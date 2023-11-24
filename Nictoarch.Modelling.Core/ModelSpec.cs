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

namespace Nictoarch.Modelling.Core
{
    public sealed class ModelSpec
    {
        private ModelSpecImpl m_spec;

        public string Name => this.m_spec.name;

        private ModelSpec(ModelSpecImpl spec)
        {
            this.m_spec = spec;
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
            DeserializerBuilder builder = new DeserializerBuilder()
                //.WithObjectFactory(new ModelRegistryObjectFactory(registry))

                //see https://github.com/aaubry/YamlDotNet/wiki/Serialization.Deserializer#withnodedeserializer
                .WithNodeDeserializer(
                    nodeDeserializerFactory: innerDeserialzier => new ValidatingDeserializer(innerDeserialzier), 
                    where: syntax => syntax.InsteadOf<ObjectNodeDeserializer>()
                )
                .WithTypeConverter(new JsonataQueryYamlConverter())
                ;

            registry.ConfigureYamlDeserialzier(builder);

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

            return new ModelSpec(spec);
        }

        public async Task<Model> GetModelAsync(CancellationToken cancellationToken = default)
        {
            List<Entity> entities = new List<Entity>();
            List<Link> links = new List<Link>();
            List<object> invalidObjects = new List<object>();

            /*
            foreach (ModelSpecImpl.ModelProviderSpec providerSpec in this.m_spec.providers)
            {
                await providerSpec.ProcessAsync(entities, links, invalidObjects, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
            }

            return new Model(this.m_spec.name, entities, links, invalidObjects);
            */

            await Task.CompletedTask;
            throw new NotImplementedException("TODO");
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
            [Required] public object extract { get; set; } = default!;
            public JsonataQuery? filter { get; set; }
            public EntitiesSelectorBase? entities { get; set; }
            public JsonataQuery? invalid { get; set; }
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
