using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Jsonata.Net.Native;

namespace Nictoarch.Modelling.K8s
{
    /**
     
    $.groups.preferredVersion.groupVersion
    $.resources["list" in verbs].name

    core: [  
      "componentstatuses",
      "configmaps",
      "endpoints",
      "events",
      "limitranges",
      "namespaces",
      "nodes",
      "persistentvolumeclaims",
      "persistentvolumes",
      "pods",
      "podtemplates",
      "replicationcontrollers",
      "resourcequotas",
      "secrets",
      "serviceaccounts",
      "services"
    ]
             
    APIGroupList
    [
        

        "apiregistration.k8s.io/v1",    -> "apiservices"
        "apps/v1",                      -> [  "controllerrevisions",  "daemonsets",  "deployments",  "replicasets",  "statefulsets"  ]
        "events.k8s.io/v1",             -> "events"
        "authentication.k8s.io/v1",     ->
        "authorization.k8s.io/v1",      ->
        "autoscaling/v2",               -> "horizontalpodautoscalers"
        "batch/v1",                     -> [  "cronjobs",  "jobs"  ]
        "certificates.k8s.io/v1",       -> "certificatesigningrequests"
        "networking.k8s.io/v1",         -> [  "ingressclasses",  "ingresses",  "networkpolicies"  ]
        "policy/v1",                    -> "poddisruptionbudgets"
        "rbac.authorization.k8s.io/v1", -> [  "clusterrolebindings",  "clusterroles",  "rolebindings",  "roles"  ]
        "storage.k8s.io/v1",            -> [  "csidrivers",  "csinodes",  "csistoragecapacities",  "storageclasses",  "volumeattachments"  ]
        "admissionregistration.k8s.io/v1", -> [  "mutatingwebhookconfigurations",  "validatingwebhookconfigurations"  ]
        "apiextensions.k8s.io/v1",      -> "customresourcedefinitions"
        "scheduling.k8s.io/v1",         -> "priorityclasses"
        "coordination.k8s.io/v1",       -> "leases"
        "node.k8s.io/v1",               -> "runtimeclasses"
        "discovery.k8s.io/v1",          -> "endpointslices"
        "flowcontrol.apiserver.k8s.io/v1beta2", -> [  "flowschemas",  "prioritylevelconfigurations"  ]
        
        "cilium.io/v2"                  -> [  "ciliumidentities",  "ciliumexternalworkloads",  "ciliumendpoints",  "ciliumnodes",  "ciliumnetworkpolicies",  "ciliumclusterwidenetworkpolicies"  ]
    ]

    */

    public sealed class ModelSpec
    {
        [JsonRequired] public string model_name { get; set; } = default!;
        [JsonRequired] public List<EntitySelector> entities { get; set; } = default!;
        [JsonRequired] public List<LinkSelector> links { get; set; } = default!;

        public abstract class SelectorBase: IJsonOnDeserialized
        {
            [JsonRequired] public string api_group { get; set; } = default!; //eg "apps" or "apps/v1". use "v1" or "core" for core resources
            [JsonRequired] public string resource_kind { get; set; } = default!; //eg "deployment" or "deployments"
            public string? @namespace { get; set; } //null or namespace name
            public string? label_query { get; set; } //null or valid label selector, see https://kubernetes.io/docs/concepts/overview/working-with-objects/labels/#label-selectors

            public string domain_id_expr { get; set; } = "metadata.uid";
            public string semantic_id_expr { get; set; } = "metadata.name & '@' & metadata.namespace";
            public string display_name_expr { get; set; } = "metadata.name & '@' & metadata.namespace";

            internal JsonataQuery domainIdQuery = default!;
            internal JsonataQuery semanticIdQuery = default!;
            internal JsonataQuery displayNameQuery = default!;

            void IJsonOnDeserialized.OnDeserialized()
            {
                if (this.@namespace == "")
                {
                    throw new Exception($"Bad value of '{nameof(this.@namespace)}': either specify a namespace name or use 'null'");
                }

                if (this.label_query == "")
                {
                    throw new Exception($"Bad value of '{nameof(this.label_query)}': either specify a valid K8s label algebra selector, or use 'null'");
                }

                try
                {
                    this.domainIdQuery = new JsonataQuery(this.domain_id_expr);
                }
                catch (Exception ex)
                {
                    throw new Exception($"Failed to parse '{nameof(this.domain_id_expr)}': {ex.Message}", ex);
                }

                try
                {
                    this.semanticIdQuery = new JsonataQuery(this.semantic_id_expr);
                }
                catch (Exception ex)
                {
                    throw new Exception($"Failed to parse '{nameof(this.semantic_id_expr)}': {ex.Message}", ex);
                }

                try
                {
                    this.displayNameQuery = new JsonataQuery(this.display_name_expr);
                }
                catch (Exception ex)
                {
                    throw new Exception($"Failed to parse '{nameof(this.display_name_expr)}': {ex.Message}", ex);
                }
            }
        }

        public sealed class EntitySelector: SelectorBase
        {
            [JsonRequired] public string entity_type { get; set; } = default!;
        }

        public sealed class LinkSelector: SelectorBase
        {
            [JsonRequired] public string from_entity_semantic_id_expr { get; set; } = "spec.from.service & '@' & metadata.namespace";
            [JsonRequired] public string to_entity_semantic_id_expr { get; set; } = "spec.to.service & '@' & spec.to.namespace";
        }

    }
}
