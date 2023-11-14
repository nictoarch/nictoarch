using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Jsonata.Net.Native.Json;
using Nictoarch.Modelling.Core.Elements;
using YamlDotNet.Serialization;

namespace Nictoarch.Modelling.Core
{
    public interface IModelProviderFactory
    {
        string Name { get; }
        void ConfigureYamlDeserialzier(DeserializerBuilder builder);
    }

    public interface IModelProviderFactory<TConfig, TEntitySpec, TValidationSpec> : IModelProviderFactory
        where TConfig : class
        where TEntitySpec : class
        where TValidationSpec : class
    {
        Task<IModelProvider> GetProviderAsync(TConfig config, CancellationToken cancellationToken);
    }

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
}
