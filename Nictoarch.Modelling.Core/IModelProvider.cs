using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Jsonata.Net.Native.Json;
using Nictoarch.Modelling.Core.Elements;

namespace Nictoarch.Modelling.Core
{
    public interface IProviderBase
    {
        string Name { get; }
    }

    public interface IEntityProvider<TEntitySpec>: IProviderBase
        where TEntitySpec : class
    {
        Task<List<Entity>> GetEntitiesAsync(TEntitySpec spec, CancellationToken cancellationToken);
    }
}
