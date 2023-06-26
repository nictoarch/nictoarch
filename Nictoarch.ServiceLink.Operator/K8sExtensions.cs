using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using k8s;
using k8s.Models;
using YamlDotNet.Core;

namespace Nictoarch.ServiceLink.Operator
{
    public static class K8sExtensions
    {
        public static async Task<T> ListNamespacedCustomObjectAsync<T>(
            this IKubernetes client,
            string group,
            string version,
            string namespaceParameter,
            string plural,
            CancellationToken cancellationToken = default
        )
        where T : IKubernetesObject
        {
            k8s.Autorest.HttpOperationResponse<object> resp = await client.CustomObjects.ListNamespacedCustomObjectWithHttpMessagesAsync(
                group: group,
                version: version,
                namespaceParameter: namespaceParameter,
                plural: plural,
                cancellationToken: cancellationToken
            ).ConfigureAwait(false);
            return KubernetesJson.Deserialize<T>(resp.Body.ToString());
        }


        public static void CheckStatus(this V1Status status)
        {
            if (status.Status != "Success")
            {
                throw new Exception($"Operation status error: {status.Status} ({status.Code}), {status.Reason}, {status.Message}");
            }
        }

        public static string GetNamespacedName(this IMetadata<V1ObjectMeta> obj)
        {
            return GetNamespacedName(obj.Metadata.Name, obj.Metadata.NamespaceProperty);
        }

        public static string GetNamespacedName(string name, string namespaceProperty)
        {
            if (String.IsNullOrWhiteSpace(namespaceProperty))
            {
                throw new ArgumentException("Empty namespace");
            }

            return $"{name}@{namespaceProperty}";
        }
    }
}
