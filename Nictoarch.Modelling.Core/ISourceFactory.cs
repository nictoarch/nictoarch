using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Jsonata.Net.Native.Json;
using Nictoarch.Modelling.Core.Spec;
using YamlDotNet.Serialization.BufferedDeserialization.TypeDiscriminators;

namespace Nictoarch.Modelling.Core
{
    public interface ISourceFactory
    {
        string Name { get; }
        IEnumerable<ITypeDiscriminator> GetYamlTypeDiscriminators();
    }

    public interface ISource : IAsyncDisposable
    {

    }

    public interface ISource<TExtractConfig> : ISource
        where TExtractConfig : ExtractConfigBase
    {
        Task<JToken> Extract(TExtractConfig extractConfig, CancellationToken cancellationToken);
    }

    public interface ISourceFactory<TSourceConfig, TSource, TExtractConfig> : ISourceFactory
        where TSourceConfig : SourceConfigBase
        where TExtractConfig : ExtractConfigBase
        where TSource : ISource<TExtractConfig>
    {
        Task<ISource> GetSource(TSourceConfig sourceConfig, CancellationToken cancellationToken);
    }

}
