using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Jsonata.Net.Native.Json;
using Nictoarch.Modelling.Core;
using Nictoarch.Modelling.Core.Elements;
using Nictoarch.Modelling.Core.Spec;
using Nictoarch.Modelling.Core.Yaml;
using YamlDotNet.Serialization.BufferedDeserialization.TypeDiscriminators;

namespace Nictoarch.Modelling.Core.BuiltinSources.Combined
{
    public sealed class CombinedSourceFactory : ISourceFactory<CombinedSourceConfig, CombinedSource, CombinedExtractConfig>
    {
        string ISourceFactory.Name => "combined";

        private readonly SourceRegistry m_registry;

        public CombinedSourceFactory(SourceRegistry registry)
        {
            this.m_registry = registry;
        }

        IEnumerable<ITypeDiscriminator> ISourceFactory.GetYamlTypeDiscriminators()
        {
            yield break;
        }

        async Task<ISource> ISourceFactory<CombinedSourceConfig, CombinedSource, CombinedExtractConfig>.GetSource(CombinedSourceConfig sourceConfig, CancellationToken cancellationToken)
        {
            JObject sourceData = await this.GetSourceData(sourceConfig, cancellationToken);
            return new CombinedSource(sourceData);
        }

        private async Task<JObject> GetSourceData(CombinedSourceConfig sourceConfig, CancellationToken cancellationToken)
        {
            JObject result = new JObject();
            foreach ((string elementName, CombinedSourceConfig.SubElementConfig sourceElementConfig) in sourceConfig.sources)
            {
                cancellationToken.ThrowIfCancellationRequested();
                JToken data = await this.GetSourceData(sourceElementConfig, cancellationToken);
                result.Add(elementName, data);
            }

            return result;
        }

        private async Task<JToken> GetSourceData(CombinedSourceConfig.SubElementConfig sourceElementConfig, CancellationToken cancellationToken)
        {
            if (!this.m_registry.GetProviderFactory(sourceElementConfig.source.type, out SourceRegistry.SourceFactoryWrapper? factory))
            {
                throw new Exception("Should not happen");
            }
            await using (ISource source = await factory.GetSource(sourceElementConfig.source, cancellationToken))
            {
                //this.OnTrace?.Invoke($"Got source");
                cancellationToken.ThrowIfCancellationRequested();

                JToken data = await factory.Extract(source, sourceElementConfig.extract, cancellationToken);
                //this.OnTrace?.Invoke($"Extracted element data:\n" + data.ToIndentedString());

                return data;
            }
        }
    }
}