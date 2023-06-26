using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using k8s.Models;

namespace Nictoarch.ServiceLink.Operator
{
    internal static class PolicyComparer
    {
        internal static bool Compare(V1NetworkPolicy expectedPolicy, V1NetworkPolicy existingPolicy)
        {
            return CompareMetadata(expectedPolicy.Metadata, existingPolicy.Metadata)
                && CompareStringList(expectedPolicy.Spec.PolicyTypes, existingPolicy.Spec.PolicyTypes)
                && CompareLabelSelector(expectedPolicy.Spec.PodSelector, existingPolicy.Spec.PodSelector)
                && CompareLists(expectedPolicy.Spec.Egress, existingPolicy.Spec.Egress, CompareEgressRule)
                && CompareLists(expectedPolicy.Spec.Ingress, existingPolicy.Spec.Ingress, CompareIngressRule);
        }

        private static bool CompareLists<T>(IList<T>? l1, IList<T>? l2, Func<T, T, bool> comparer)
        {
            if (l1 == null && l2 == null)
            {
                return true;
            }
            if (l1 == null || l2 == null)
            {
                return false;
            }
            if (l1.Count != l2.Count)
            {
                return false;
            }
            for (int i = 0; i < l1.Count; ++i)
            {
                if (!comparer(l1[i], l2[i]))
                {
                    return false;
                }
            }
            return true;
        }

        private static bool CompareStringList(IList<string>? l1, IList<string>? l2)
        {
            return CompareLists(l1, l2, (s1, s2) => s1 == s2);
        }

        private static bool CompareDictionary(IDictionary<string, string>? d1, IDictionary<string, string>? d2)
        {
            if (d1 == null && d2 == null)
            {
                return true;
            }
            if (d1 == null || d2 == null)
            {
                return false;
            }
            if (d1.Count != d2.Count)
            {
                return false;
            }
            foreach (KeyValuePair<string, string> p1 in d1)
            {
                if (!d2.TryGetValue(p1.Key, out string? v2))
                {
                    return false;
                }
                if (p1.Value != v2)
                {
                    return false;
                }
            }
            return true;
        }

        private static bool CompareMetadata(V1ObjectMeta metadata1, V1ObjectMeta metadata2)
        {
            return metadata1.Name == metadata2.Name
                && metadata1.NamespaceProperty == metadata2.NamespaceProperty
                && CompareDictionary(metadata1.Labels, metadata2.Labels);
        }

        private static bool CompareEgressRule(V1NetworkPolicyEgressRule? rule1, V1NetworkPolicyEgressRule? rule2)
        {
            if (rule1 == null && rule2 == null)
            {
                return true;
            }
            if (rule1 == null || rule2 == null)
            {
                return false;
            }
            return CompareLists(rule1.Ports, rule2.Ports, ComparePort)
                && CompareLists(rule1.To, rule2.To, ComparePeer);
        }

        private static bool CompareIngressRule(V1NetworkPolicyIngressRule? rule1, V1NetworkPolicyIngressRule? rule2)
        {
            if (rule1 == null && rule2 == null)
            {
                return true;
            }
            if (rule1 == null || rule2 == null)
            {
                return false;
            }
            return CompareLists(rule1.Ports, rule2.Ports, ComparePort)
                && CompareLists(rule1.FromProperty, rule2.FromProperty, ComparePeer);
        }

        private static bool ComparePeer(V1NetworkPolicyPeer? peer1, V1NetworkPolicyPeer? peer2)
        {
            if (peer1 == null && peer2 == null)
            {
                return true;
            }
            if (peer1 == null || peer2 == null)
            {
                return false;
            }

            return CompareIp(peer1.IpBlock, peer2.IpBlock)
                && CompareLabelSelector(peer1.NamespaceSelector, peer2.NamespaceSelector)
                && CompareLabelSelector(peer1.PodSelector, peer2.PodSelector);
        }

        private static bool CompareIp(V1IPBlock? ipBlock1, V1IPBlock? ipBlock2)
        {
            if (ipBlock1 == null && ipBlock2 == null)
            {
                return true;
            }
            if (ipBlock1 == null || ipBlock2 == null)
            {
                return false;
            }
            return ipBlock1.Cidr == ipBlock2.Cidr
                && CompareStringList(ipBlock1.Except, ipBlock2.Except);
        }

        private static bool ComparePort(V1NetworkPolicyPort? port1, V1NetworkPolicyPort? port2)
        {
            if (port1 == null && port2 == null)
            {
                return true;
            }
            if (port1 == null || port2 == null)
            {
                return false;
            }

            return port1.Port == port2.Port //has comparer
                && (port1.Protocol ?? "TCP") == (port2.Protocol ?? "TCP")
                && port1.EndPort == port2.EndPort;
        }

        private static bool CompareLabelSelector(V1LabelSelector? selector1, V1LabelSelector? selector2)
        {
            if (selector1 == null && selector2 == null)
            {
                return true;
            }
            if (selector1 == null || selector2 == null)
            {
                return false;
            }
            return CompareLists(selector1.MatchExpressions, selector2.MatchExpressions, CompareLabelSelectorReq)
                && CompareDictionary(selector1.MatchLabels, selector2.MatchLabels);
        }

        private static bool CompareLabelSelectorReq(V1LabelSelectorRequirement? req1, V1LabelSelectorRequirement? req2)
        {
            if (req1 == null && req2 == null)
            {
                return true;
            }
            if (req1 == null || req2 == null)
            {
                return false;
            }
            return req1.OperatorProperty == req2.OperatorProperty
                    && req1.Key == req2.Key
                    && CompareStringList(req1.Values, req2.Values);
        }
    }
}
