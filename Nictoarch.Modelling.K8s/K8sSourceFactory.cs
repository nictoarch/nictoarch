using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using k8s;
using Nictoarch.Modelling.Core;
using Nictoarch.Modelling.Core.Yaml;
using YamlDotNet.Serialization.BufferedDeserialization.TypeDiscriminators;

namespace Nictoarch.Modelling.K8s
{
    public sealed class K8sSourceFactory : ISourceFactory<K8sSourceConfig, K8sSource, K8sExtractConfig>
    {
        string ISourceFactory.Name => "k8s";

        IEnumerable<ITypeDiscriminator> ISourceFactory.GetYamlTypeDiscriminators()
        {
            yield return new StrictKeyValueTypeDiscriminator(
                baseType: typeof(K8sSourceConfig.Config),
                targetKey: nameof(K8sSourceConfig.Config.type),
                typeMapping: new Dictionary<string, Type> {
                    { K8sSourceConfig.ClusterConfig.TYPE, typeof(K8sSourceConfig.ClusterConfig) },
                    { K8sSourceConfig.FileConfig.TYPE, typeof(K8sSourceConfig.FileConfig) },
                }
            );
        }

        async Task<ISource> ISourceFactory<K8sSourceConfig, K8sSource, K8sExtractConfig>.GetSource(K8sSourceConfig sourceConfig, CancellationToken cancellationToken)
        {
            KubernetesClientConfiguration k8sConfig = K8sClient.GetConfiguration(
                useClusterConfig: sourceConfig.config is K8sSourceConfig.ClusterConfig,
                configFile: (sourceConfig.config as K8sSourceConfig.FileConfig)?.file, 
                httpClientTimeoutSeconds: sourceConfig.connect_timeout_seconds
            );
            K8sClient client = new K8sClient(k8sConfig);
            await client.InitAsync(cancellationToken);
            K8sSource source = new K8sSource(client);
            return source;
        }
    }
}
