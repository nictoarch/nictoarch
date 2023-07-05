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

        private async Task<List<Entity>> GetEntitiesAsync(CancellationToken cancellationToken)
        {
            List<Entity> result = new List<Entity>();
            foreach (ModelSpecImpl.EntitySelector entitySelector in this.m_spec.entities)
            {
                List<Entity> chunk = await entitySelector.GetEntitiesAsync(cancellationToken);
                result.AddRange(chunk);
                cancellationToken.ThrowIfCancellationRequested();
            }
            return result;
        }

        public async Task<Model> GetModelAsync(CancellationToken cancellationToken = default)
        {
            List<Entity> entities = await this.GetEntitiesAsync(cancellationToken);
            return new Model(this.m_spec.name, entities, new List<Link>());
        }
    }
}
