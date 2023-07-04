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

    
}
