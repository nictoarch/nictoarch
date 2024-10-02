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
        public static Task<T> ListCustomObjectForAllNamespacesAsync<T>(
            this ICustomObjectsOperations operations,
            string group,
            string version,
            string plural,
            bool? allowWatchBookmarks = null,
            string? continueParameter = null,
            string? fieldSelector = null,
            string? labelSelector = null,
            int? limit = null,
            string? resourceVersion = null,
            string? resourceVersionMatch = null,
            int? timeoutSeconds = null,
            bool? watch = null,
            bool? pretty = null,
            CancellationToken cancellationToken = default
        )
        {
            //see https://github.com/kubernetes-client/csharp/discussions/1589
            return operations.ListClusterCustomObjectAsync<T>(
                group, version, plural, 
                allowWatchBookmarks, continueParameter, fieldSelector, labelSelector, limit,
                resourceVersion, resourceVersionMatch, 
                timeoutSeconds, watch, pretty, cancellationToken
            );
        }

        public static Task<k8s.Autorest.HttpOperationResponse<T>> ListCustomObjectForAllNamespacesWithHttpMessagesAsync<T>(
            this ICustomObjectsOperations customObjects,
            string group,
            string version,
            string plural,
            bool? allowWatchBookmarks = null,
            string? continueParameter = null,
            string? fieldSelector = null,
            string? labelSelector = null,
            int? limit = null,
            string? resourceVersion = null,
            string? resourceVersionMatch = null,
            int? timeoutSeconds = null,
            bool? watch = null,
            bool? pretty = null,
            IReadOnlyDictionary<string, IReadOnlyList<string>>? customHeaders = null,
            CancellationToken cancellationToken = default
        )
        {
            //see https://github.com/kubernetes-client/csharp/discussions/1589
            return customObjects.ListClusterCustomObjectWithHttpMessagesAsync<T>(
                group, version, plural, 
                allowWatchBookmarks, continueParameter, 
                fieldSelector, labelSelector,
                limit, resourceVersion, resourceVersionMatch,
                timeoutSeconds, watch, pretty,
                customHeaders, cancellationToken);
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
