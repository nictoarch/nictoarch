using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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

        public static ModelSpec LoadFromFile(string fileName, ModelProviderRegistry registry)
        {
            using (StreamReader reader = new StreamReader(fileName))
            {
                return Load(reader, registry);
            }
        }

        public static ModelSpec Load(TextReader reader, ModelProviderRegistry registry)
        {
            IDeserializer deserializer = new DeserializerBuilder()
                .WithObjectFactory(new ModelRegistryObjectFactory(registry))
                .WithNodeDeserializer(inner => new ValidatingDeserializer(inner), s => s.InsteadOf<ObjectNodeDeserializer>())
                .Build();

            ModelSpecImpl spec = deserializer.Deserialize<ModelSpecImpl>(reader);

            return new ModelSpec(spec);
        }

        public async Task<Model> GetModelAsync(CancellationToken cancellationToken = default)
        {
            List<Entity> entities = new List<Entity>();
            List<Link> links = new List<Link>();
            List<object> invalidObjects = new List<object>();

            foreach (ModelSpecImpl.ModelProviderSpec providerSpec in this.m_spec.providers)
            {
                await providerSpec.ProcessAsync(entities, links, invalidObjects, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
            }

            return new Model(this.m_spec.name, entities, links, invalidObjects);
        }
    }
}
