using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using k8s.Models;
using k8s;

namespace Nictoarch.ServiceLink.Operator.Resources
{
    public sealed class LinkResource : CustomResource<LinkResourceSpec, LinkResourceStatus>
    {
        public override string ToString()
        {
            return $"{this.Metadata.Name}, {this.Status}";
        }

        internal string GetEgressPolicyName()
        {
            return $"servicelink_egress_{this.Metadata.Name}";
        }

        internal string GetEgressPolicyNamespace()
        {
            return this.Metadata.NamespaceProperty;
        }

        internal string GetEgressPolicyNamespacedName()
        {
            return K8sExtensions.GetNamespacedName(this.GetEgressPolicyName(), this.GetEgressPolicyNamespace());
        }

        internal string GetEgressPolicyServiceNamespacedName()
        {
            return K8sExtensions.GetNamespacedName(this.Spec.from.service, this.GetEgressPolicyNamespace());
        }

        internal string GetIngressPolicyName()
        {
            return $"servicelink_ingress_{this.Metadata.NamespaceProperty}_{this.Metadata.Name}";
        }

        internal string GetIngressPolicyNamespace()
        {
            return this.Spec.to.namespaceProperty ?? this.Metadata.NamespaceProperty;
        }

        internal string GetIngressPolicyNamespacedName()
        {
            return K8sExtensions.GetNamespacedName(this.GetIngressPolicyName(), this.GetIngressPolicyNamespace());
        }

        internal string GetIngressPolicyServiceNamespacedName()
        {
            return K8sExtensions.GetNamespacedName(this.Spec.to.service, this.GetIngressPolicyNamespace());
        }

        internal bool UpdateState(Controller.ServiceState egressServiceState, Controller.ServiceState ingressServiceState, Controller.PolicyState egressState, Controller.PolicyState ingressState)
        {
            //TODO:
            throw new NotImplementedException("TODO");
        }
    }

    public sealed class LinkResourceSpec
    {
        public From from { get; set; } = default!;
        public To to { get; set; } = default!;

        public sealed class From
        {
            public string service { get; set; } = default!;
        }

        public sealed class To
        {
            [JsonPropertyName("namespace")]
            public string? namespaceProperty { get; set; }

            public string service { get; set; } = default!;

            public IntstrIntOrString port { get; set; } = default!;

            //see https://kubernetes.io/docs/reference/generated/kubernetes-api/v1.26/#networkpolicyport-v1-networking-k8s-io
            public string? protocol { get; set; }

            //TODO: support endPort, see https://kubernetes.io/docs/reference/generated/kubernetes-api/v1.26/#networkpolicyport-v1-networking-k8s-io
            //TODO: add validation
        }
    }

    public sealed class LinkResourceStatus : V1Status
    {
        public string state { get; set; } = default!;

        public override string ToString()
        {
            return "state: " + state;
        }
    }

    public sealed class LinkResourceList: KubernetesList<LinkResource>, IKubernetesObject<V1ListMeta>, IItems<LinkResource>
    {
        public LinkResourceList(IList<LinkResource> items, string apiVersion = default!, string kind = default!, V1ListMeta metadata = default!) 
            : base(items, apiVersion, kind, metadata)
        {
        }
    }
}
