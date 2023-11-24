﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Jsonata.Net.Native.Json;
using Jsonata.Net.Native;
using k8s;
using Nictoarch.Modelling.Core;
using Nictoarch.Modelling.Core.Elements;
using Nictoarch.Modelling.K8s.Spec;
using YamlDotNet.Serialization;

namespace Nictoarch.Modelling.K8s
{
    public sealed class K8sModelProviderFactory : ISourceFactory<ProviderConfig, EntitySelector, ValidationSelector>
    {
        string ISourceFactory.Name => "k8s";

        void ISourceFactory.ConfigureYamlDeserialzier(DeserializerBuilder builder)
        {
            //nothing to do
        }

        Task<IModelProvider> ISourceFactory<ProviderConfig, EntitySelector, ValidationSelector>.GetProviderAsync(ProviderConfig config, CancellationToken cancellationToken)
        {
            KubernetesClientConfiguration k8sConfig = config.GetK8sConfiguration();
            List<Entity> results = new List<Entity>();
            K8sClient client = new K8sClient(k8sConfig);
            IModelProvider provider = new K8sModelProvider(client);
            return Task.FromResult(provider);
        }
    }
}
