using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Jsonata.Net.Native.Json;
using Nictoarch.Modelling.Core;

namespace Nictoarch.Modelling.Core.BuiltinSources.Combined
{
    public sealed class CombinedSource : ISource<CombinedExtractConfig>
    {
        private readonly JObject m_data;

        public CombinedSource(JObject data)
        {
            this.m_data = data;
        }

        ValueTask IAsyncDisposable.DisposeAsync()
        {
            //nothing to do
            return ValueTask.CompletedTask;
        }

        Task<JToken> ISource<CombinedExtractConfig>.Extract(CombinedExtractConfig extractConfig, CancellationToken cancellationToken)
        {
            return Task.FromResult((JToken)this.m_data);
        }
    }
}
