using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Jsonata.Net.Native.Json;
using k8s;
using Nictoarch.Modelling.Core;

namespace Nictoarch.Modelling.K8s
{
    internal sealed class K8sSource: ISource<K8sExtractConfig>
    {
        private readonly K8sClient m_client;

        internal K8sSource(KubernetesClientConfiguration configuration)
        {
            this.m_client = new K8sClient(configuration);
        }

        ValueTask IAsyncDisposable.DisposeAsync()
        {
            this.m_client.Dispose();
            return ValueTask.CompletedTask;
        }

        async Task<JToken> ISource<K8sExtractConfig>.Extract(K8sExtractConfig extractConfig, CancellationToken cancellationToken)
        {
            JArray resources = await this.m_client.GetResources(
                apiGroup: extractConfig.api_group,
                resourceKind: extractConfig.resource_kind.ToLowerInvariant(),
                @namespace: extractConfig.@namespace,
                labelSelector: extractConfig.label_query,
                cancellationToken: cancellationToken
            );

            return resources;
        }
    }
}
