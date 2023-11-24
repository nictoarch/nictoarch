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

namespace Nictoarch.Modelling.Core
{
    public interface ISourceFactory
    {
        string Name { get; }
        void AddYamlTypeDiscriminators(ITypeDiscriminatingNodeDeserializerOptions opts);
    }

    public interface ISourceFactory<TConfig> : ISourceFactory
        where TConfig : ModelSpec.SourceConfigBase
    {
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
