using k8s;
using k8s.Models;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Nictoarch.ServiceLink.Operator.Resources
{
    public abstract class CustomResource : KubernetesObject, IMetadata<V1ObjectMeta>
    {
        [JsonPropertyName("metadata")]
        public V1ObjectMeta Metadata { get; set; } = default!;
    }

    public abstract class CustomResource<TSpec, TStatus> : CustomResource
    {
        [JsonPropertyName("spec")]
        public TSpec Spec { get; set; } = default!;

        [JsonPropertyName("status")]
        public TStatus Status { get; set; } = default!;
    }

    public class CustomResourceList<T> : KubernetesObject
        where T : CustomResource
    {
        public V1ListMeta Metadata { get; set; } = default!;
        public List<T> Items { get; set; } = default!;
    }
}