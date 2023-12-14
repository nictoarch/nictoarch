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
        Task<JToken> Extract(TExtractConfig extractConfig);
    }

    public interface ISourceFactory<TSourceConfig, TSource, TExtractConfig> : ISourceFactory
        where TSourceConfig : ModelSpec.SourceConfigBase
        where TExtractConfig: ModelSpec.ExtractConfigBase
        where TSource: ISource<TExtractConfig>
    {
        Task<TSource> GetSource(TSourceConfig sourceConfig);
        //Task<IModelProvider> GetProviderAsync(TConfig config, CancellationToken cancellationToken);
    }

    /*
    public interface IModelProvider: IDisposable
    {
    }

    public interface IModelProvider<TEntitySpec, TValidationSpec>: IModelProvider
        where TEntitySpec : class
        where TValidationSpec : class
    {
        Task<List<Entity>> GetEntitiesAsync(TEntitySpec spec, CancellationToken cancellationToken);
        Task<List<object>> GetInvalidObjectsAsync(TValidationSpec spec, CancellationToken cancellationToken);
    }
    */
}
