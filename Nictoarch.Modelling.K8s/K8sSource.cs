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

        internal K8sSource(K8sClient client)
        {
            this.m_client = client;
        }

        ValueTask IAsyncDisposable.DisposeAsync()
        {
            this.m_client.Dispose();
            return ValueTask.CompletedTask;
        }

        async Task<JToken> ISource<K8sExtractConfig>.Extract(K8sExtractConfig extractConfig, CancellationToken cancellationToken)
        {
            switch (extractConfig.type)
            {
            case K8sExtractConfig.EDataType.version:
                JToken result = await this.m_client.GetVersion(cancellationToken);
                return result;
            case K8sExtractConfig.EDataType.resource:
                JArray resources = await this.m_client.GetResources(
                    apiGroup: extractConfig.api_group,
                    resourceKind: extractConfig.resource_kind!.ToLowerInvariant(),
                    @namespace: extractConfig.@namespace,
                    labelSelector: extractConfig.label_query,
                    cancellationToken: cancellationToken
                );
                return resources;
            default:
                throw new Exception($"should not happen. Check validation in {nameof(K8sExtractConfig)}.{nameof(K8sExtractConfig.OnDeserialized)}()");
            }

        }
    }
}
