using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using k8s;
using Nictoarch.Modelling.Core;
using YamlDotNet.Serialization.BufferedDeserialization.TypeDiscriminators;

namespace Nictoarch.Modelling.K8s
{
    public sealed class K8sSourceFactory : ISourceFactory<K8sSourceConfig, K8sSource, K8sExtractConfig>
    {
        string ISourceFactory.Name => "k8s";

        IEnumerable<ITypeDiscriminator> ISourceFactory.GetYamlTypeDiscriminators()
        {
            //nothing to do
            yield break;
        }

        Task<ISource> ISourceFactory<K8sSourceConfig, K8sSource, K8sExtractConfig>.GetSource(K8sSourceConfig sourceConfig, CancellationToken cancellationToken)
        {
            KubernetesClientConfiguration k8sConfig = K8sClient.GetConfiguration(sourceConfig.config_file, sourceConfig.connect_timeout_seconds);
            K8sSource source = new K8sSource(k8sConfig);
            return Task.FromResult((ISource)source);
        }
    }
}
