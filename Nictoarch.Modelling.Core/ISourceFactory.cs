using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Jsonata.Net.Native.Json;
using Nictoarch.Modelling.Core.Elements;
using Nictoarch.Modelling.Core.Yaml;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.BufferedDeserialization;
using YamlDotNet.Serialization.BufferedDeserialization.TypeDiscriminators;

namespace Nictoarch.Modelling.Core
{
    public interface ISourceFactory
    {
        string Name { get; }
        IEnumerable<ITypeDiscriminator> GetYamlTypeDiscriminators();
    }

    public interface ISource: IAsyncDisposable
    {

    }

    public interface ISource<TExtractConfig>: ISource
        where TExtractConfig : ModelSpec.ExtractConfigBase
    {
        Task<JToken> Extract(TExtractConfig extractConfig, CancellationToken cancellationToken);
    }

    public interface ISourceFactory<TSourceConfig, TSource, TExtractConfig> : ISourceFactory
        where TSourceConfig : ModelSpec.SourceConfigBase
        where TExtractConfig: ModelSpec.ExtractConfigBase
        where TSource: ISource<TExtractConfig>
    {
        Task<ISource> GetSource(TSourceConfig sourceConfig, CancellationToken cancellationToken);
    }

}
