using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using IdentityModel.OidcClient;
using k8s;
using k8s.Autorest;
using k8s.Models;
using Nictoarch.Common;
using Nictoarch.ServiceLink.Operator.Resources;
using YamlDotNet.Core;
using YamlDotNet.Core.Tokens;

namespace Nictoarch.ServiceLink.Operator
{
    internal sealed class Controller : IAsyncDisposable
    {
        internal const string GROUP = "servicelink.nictoarch.io";
        private const string VERSION = "v1";
        private const string PLURAL = "links";
        internal const string POLICY_LABEL = GROUP + "/from-link";
        internal const string K8S_MANAGED_BY_LABEL = "app.kubernetes.io/managed-by";
        internal const string K8S_NAME_LABEL = "kubernetes.io/metadata.name";
        private readonly Settings m_settings;

        //all keys are NamespacedNames, see K8sExtensions.GetNamespacedName()
        private readonly Dictionary<string, V1Service> m_servicesReported = new Dictionary<string, V1Service>();
        private readonly Dictionary<string, V1NetworkPolicy> m_policiesReported = new Dictionary<string, V1NetworkPolicy>();
        private readonly Dictionary<string, LinkResource> m_linksReported = new Dictionary<string, LinkResource>();

        private readonly Dictionary<string, RequestedPolicy> m_policiesRequested = new Dictionary<string, RequestedPolicy>();

        private readonly Channel<EventWrapper> m_channel = Channel.CreateUnbounded<EventWrapper>();

        private readonly CancellationTokenSource m_tasksStopTokenSource = new CancellationTokenSource();
        private readonly CancellationTokenSource m_stopTokenSource = new CancellationTokenSource();
        private readonly IKubernetes m_client;
        private Task m_bgTasks = Task.CompletedTask;
        private Task m_catchBgTasksErrorTask = Task.CompletedTask;

        internal Controller(Settings settings)
        {
            this.m_settings = settings;

            KubernetesClientConfiguration config;
            if (KubernetesClientConfiguration.IsInCluster())
            {
                config = KubernetesClientConfiguration.InClusterConfig();
            }
            else
            {
                config = KubernetesClientConfiguration.BuildConfigFromConfigFile();
            }

            this.m_client = new Kubernetes(config);
        }

        internal void Stop()
        {
            this.m_stopTokenSource.Cancel();
        }

        internal async Task<IDisposable> InitAsync()
        {
            V1ServiceList serviceList = await this.m_client.CoreV1.ListServiceForAllNamespacesAsync();
            Task<HttpOperationResponse<V1ServiceList>> serviceListWatchTask = this.m_client.CoreV1.ListServiceForAllNamespacesWithHttpMessagesAsync(
                watch: true,
                resourceVersion: serviceList!.Metadata.ResourceVersion,
                cancellationToken: this.m_tasksStopTokenSource.Token
            );
            foreach (V1Service service in serviceList.Items)
            {
                this.m_servicesReported.Add(service.Metadata.Uid, service);
            }

            V1NetworkPolicyList policiesList = await this.m_client.NetworkingV1.ListNetworkPolicyForAllNamespacesAsync(
                labelSelector: POLICY_LABEL
            );
            Task<HttpOperationResponse<V1NetworkPolicyList>> policyListWatchTask = this.m_client.NetworkingV1.ListNetworkPolicyForAllNamespacesWithHttpMessagesAsync(
                labelSelector: POLICY_LABEL,
                watch: true,
                resourceVersion: policiesList!.Metadata.ResourceVersion,
                cancellationToken: this.m_tasksStopTokenSource.Token
            );
            foreach (V1NetworkPolicy policy in policiesList.Items)
            {
                this.m_policiesReported.Add(policy.Metadata.Uid, policy);
            }

            CustomResourceList<LinkResource> linksList = await this.m_client.CustomObjects.ListCustomObjectForAllNamespacesAsync<CustomResourceList<LinkResource>>(
                group: GROUP,
                version: VERSION,
                plural: PLURAL
            );
            Task<HttpOperationResponse<object>> linkListWatchTask = this.m_client.CustomObjects.ListCustomObjectForAllNamespacesWithHttpMessagesAsync(
                group: GROUP,
                version: VERSION,
                plural: PLURAL,
                watch: true,
                resourceVersion: linksList!.Metadata.ResourceVersion,
                cancellationToken: this.m_tasksStopTokenSource.Token
            );
            foreach (LinkResource link in linksList.Items)
            {
                this.m_linksReported.Add(link.Metadata.Uid, link);
            }

            Task[] allBgTasks = new Task[] { serviceListWatchTask, policyListWatchTask, linkListWatchTask };
            this.m_bgTasks = Task.WhenAll(allBgTasks);
            this.m_catchBgTasksErrorTask = Task.WhenAny(allBgTasks)
                .ContinueWith((t) => {
                    Task firstFinishedTask = t.Result;
                    if (firstFinishedTask.IsFaulted)
                    {
                        //TODO: log something
                        ProgramHelper.HandleFatalException(firstFinishedTask.Exception!, ProgramHelper.ExceptionSource.MainAction_Exception);
                    }
                });

            return new WatcherCollection(
                serviceListWatchTask.Watch<V1Service, V1ServiceList>(onEvent: OnEvent, onError: OnWatcherError),
                policyListWatchTask.Watch<V1NetworkPolicy, V1NetworkPolicyList>(onEvent: OnEvent, onError: OnWatcherError),
                linkListWatchTask.Watch<LinkResource, object>(onEvent: OnEvent, onError: OnWatcherError)
            );
        }

        private void OnWatcherError(Exception ex)
        {
            //TODO: log something
            ProgramHelper.HandleFatalException(ex, ProgramHelper.ExceptionSource.MainAction_Exception);
        }

        private void OnEvent<T>(WatchEventType eventType, T resource)
            where T : IMetadata<V1ObjectMeta>
        {
            this.m_channel.Writer.TryWrite(new EventWrapper(eventType, resource));
        }

        internal async Task RunAsync()
        {
            while (!this.m_stopTokenSource.IsCancellationRequested)
            {
                await this.Reconcile();

                try
                {
                    await this.ReadEventsAsync();
                }
                catch (OperationCanceledException) when (this.m_stopTokenSource.IsCancellationRequested)
                {
                    break;
                }
            }
        }

        private async Task ReadEventsAsync()
        {
            await this.m_channel.Reader.WaitToReadAsync(this.m_stopTokenSource.Token);
            //stopping would throw and interrupt

            while (true) //TODO: maybe add some limit on iterations count?
            {
                while (this.m_channel.Reader.TryRead(out EventWrapper? eventWrapper))
                {
                    this.ApplyEvent(eventWrapper);
                }

                using (CancellationTokenSource timeoutSource = new CancellationTokenSource(this.m_settings.BatchEventDelayMs))
                {
                    try
                    {
                        await this.m_channel.Reader.WaitToReadAsync(timeoutSource.Token);
                    }
                    catch (OperationCanceledException) when (timeoutSource.IsCancellationRequested)
                    {
                        //timed out
                        break;
                    }
                }
            }
        }

        private void ApplyEvent(EventWrapper eventWrapper)
        {
            switch (eventWrapper.obj)
            {
            case V1Service service:
                this.ApplyEvent(eventWrapper.eventType, service, this.m_servicesReported);
                break;
            case V1NetworkPolicy policy:
                this.ApplyEvent(eventWrapper.eventType, policy, this.m_policiesReported);
                break;
            case LinkResource link:
                this.ApplyEvent(eventWrapper.eventType, link, this.m_linksReported);
                break;
            default:
                throw new Exception("Unexpected type " + eventWrapper.obj.GetType().Name);
            }
        }

        private void ApplyEvent<T>(WatchEventType eventType, T obj, Dictionary<string, T> objects)
            where T : IMetadata<V1ObjectMeta>
        {
            string key = obj.GetNamespacedName();
            switch (eventType)
            {
            case WatchEventType.Bookmark:
                //nothing to do, right?
                break;
            case WatchEventType.Added:
                objects.Add(key, obj);
                break;
            case WatchEventType.Modified:
                objects[key] = obj;
                break;
            case WatchEventType.Deleted:
                objects.Remove(key); //ignoring results
                break;
            default:
                throw new Exception("Unexpected event type " + eventType);
            }
        }

        private async Task Reconcile()
        {
            //remove expired requests
            {
                DateTime now = DateTime.Now;
                List<string> requestsToRemove = this.m_policiesRequested
                    .Where(pair => pair.Value.expireAt < now)
                    .Select(pair => pair.Key)
                    .ToList();
                foreach (string request in requestsToRemove)
                {
                    this.m_policiesRequested.Remove(request);
                }
            }


            //main check
            HashSet<string> checkedPolicies = new HashSet<string>(this.m_linksReported.Count * 2);
            foreach (LinkResource link in this.m_linksReported.Values)
            {
                string egressPolicyNamespacedName = link.GetEgressPolicyNamespacedName();
                string ingressPolicyNamespacedName = link.GetIngressPolicyNamespacedName();
                ServiceState egressServiceState = this.GetService(link.GetEgressPolicyServiceNamespacedName(), out V1Service? egressService);
                ServiceState ingressServiceState = this.GetService(link.GetIngressPolicyServiceNamespacedName(), out V1Service? ingressService);
                PolicyState egressPolicyState;
                PolicyState ingressPolicyState;

                if (egressServiceState != ServiceState.Ok || ingressServiceState != ServiceState.Ok)
                {
                    //only if both services exist and have selectors, we are able to construct a policy (for now)
                    await this.DeletePolicyIfExistsAsync(egressPolicyNamespacedName);
                    await this.DeletePolicyIfExistsAsync(ingressPolicyNamespacedName);
                    if (!checkedPolicies.Add(egressPolicyNamespacedName))
                    {
                        throw new Exception("Checking same policy twice: " + egressPolicyNamespacedName);
                    }
                    if (!checkedPolicies.Add(ingressPolicyNamespacedName))
                    {
                        throw new Exception("Checking same policy twice: " + ingressPolicyNamespacedName);
                    }
                    egressPolicyState = PolicyState.Removed;
                    ingressPolicyState = PolicyState.Removed;
                }
                else
                {
                    egressPolicyState = await this.ReconcileNetworkPolicyAsync(egressPolicyNamespacedName, checkedPolicies, this.CreateEgressPolicy(link, egressService!, ingressService!));
                    ingressPolicyState = await this.ReconcileNetworkPolicyAsync(ingressPolicyNamespacedName, checkedPolicies, this.CreateIngressPolicy(link, egressService!, ingressService!));
                }

                if (link.UpdateState(egressServiceState, ingressServiceState, egressPolicyState, ingressPolicyState))
                {
                    using (CancellationTokenSource timeoutSource = new CancellationTokenSource(this.m_settings.OperationTimeoutMs))
                    {
                        object result = await this.m_client.CustomObjects.ReplaceNamespacedCustomObjectStatusAsync(
                            body: link,
                            group: GROUP,
                            version: VERSION,
                            namespaceParameter: link.Metadata.Name,
                            plural: PLURAL,
                            name: link.Metadata.Name
                        );
                        //here result is probably an updated link
                        //TODO: update resource version = update link in the m_reportedLinks or wait for it to come from server reports
                    }
                }
            }

            //delete stray policies
            foreach (KeyValuePair<string, V1NetworkPolicy> policyPair in this.m_policiesReported)
            {
                string key = policyPair.Key;
                if (checkedPolicies.Contains(key))
                {
                    continue;
                }

                V1NetworkPolicy policy = policyPair.Value;
                if (this.m_policiesRequested.TryGetValue(key, out RequestedPolicy? requested)
                    && requested.deleteRequested
                )
                {
                    //delete already requested
                    continue;
                }
                await this.DeletePolicyAsync(key, policy);
            }
        }

        private async Task DeletePolicyAsync(string policyNamespacedName, V1NetworkPolicy policy)
        {
            using (CancellationTokenSource timeoutSource = new CancellationTokenSource(this.m_settings.OperationTimeoutMs))
            {
                this.m_policiesRequested[policyNamespacedName] = new RequestedPolicy() {
                    deleteRequested = true,
                    policy = policy,
                    expireAt = DateTime.Now.AddSeconds(this.m_settings.RequestExpirationSeconds)
                };
                V1Status status = await this.m_client.NetworkingV1.DeleteNamespacedNetworkPolicyAsync(
                    name: policy.Metadata.Name,
                    namespaceParameter: policy.Metadata.NamespaceProperty,
                    gracePeriodSeconds: 0,
                    cancellationToken: timeoutSource.Token
                );
                status.CheckStatus();
            }
        }

        private async Task DeletePolicyIfExistsAsync(string policyNamespacedName)
        {
            V1NetworkPolicy? policyToDelete = null;
            if (this.m_policiesRequested.TryGetValue(policyNamespacedName, out RequestedPolicy? requested))
            {
                if (!requested.deleteRequested)
                {
                    policyToDelete = requested.policy;
                }
            }
            else if (this.m_policiesReported.TryGetValue(policyNamespacedName, out V1NetworkPolicy? reported))
            {
                policyToDelete = reported;
            }

            if (policyToDelete != null)
            {
                await this.DeletePolicyAsync(policyNamespacedName, policyToDelete);
            }
        }

        private ServiceState GetService(string serviceNamespacedName, out V1Service? service)
        {
            if (!this.m_servicesReported.TryGetValue(serviceNamespacedName, out service))
            {
                return ServiceState.NotFound;
            }
            else if (service.Spec.Selector == null || service.Spec.Selector.Count == 0)
            {
                return ServiceState.NoSelector;
            }
            else
            {
                return ServiceState.Ok;
            }
        }

        private async Task<PolicyState> ReconcileNetworkPolicyAsync(string policyNamespacedName, HashSet<string> checkedPolicies, V1NetworkPolicy expectedPolicy)
        {
            if (!checkedPolicies.Add(policyNamespacedName))
            {
                throw new Exception("Checking same policy twice: " + policyNamespacedName);
            }

            //1. not reported, not requested -> request
            //2. not reported, requested, requested consistent -> no op
            //3. not reported, requested, requested inconsistent -> request
            //4. reported, reported consistent -> no op
            //5. reported, reported inconsistent, not requested -> request
            //6. reported, reported inconsistent, requested, requested consistent -> no op
            //7. reported, reported inconsistent, requested, requested inconsistent -> request

            V1NetworkPolicy? oldPolicy = null;

            if (this.m_policiesRequested.TryGetValue(policyNamespacedName, out RequestedPolicy? requestedPolicy))
            {
                if (!requestedPolicy.deleteRequested)
                {
                    if (PolicyComparer.Compare(expectedPolicy, requestedPolicy.policy))
                    {
                        //no need to do anything, already requested consistent egress policy
                        return PolicyState.Consistent;
                    }
                    else
                    {
                        oldPolicy = requestedPolicy.policy;
                    }
                }
            }
            else if (this.m_policiesReported.TryGetValue(policyNamespacedName, out V1NetworkPolicy? reportedPolicy))
            {
                if (PolicyComparer.Compare(expectedPolicy, reportedPolicy))
                {
                    //no need to change current policy
                    return PolicyState.Consistent;
                }
                else
                {
                    oldPolicy = reportedPolicy;
                }
            }

            using (CancellationTokenSource timeoutSource = new CancellationTokenSource(this.m_settings.OperationTimeoutMs))
            {
                V1NetworkPolicy createdPolicy;
                if (oldPolicy != null)
                {
                    expectedPolicy.Metadata.ResourceVersion = oldPolicy.Metadata.ResourceVersion;
                    createdPolicy = await this.m_client.NetworkingV1.ReplaceNamespacedNetworkPolicyAsync(
                        body: expectedPolicy,
                        name: expectedPolicy.Metadata.Name,
                        namespaceParameter: expectedPolicy.Metadata.NamespaceProperty,
                        cancellationToken: timeoutSource.Token
                    );
                }
                else
                {
                    createdPolicy = await this.m_client.NetworkingV1.CreateNamespacedNetworkPolicyAsync(
                        body: expectedPolicy,
                        namespaceParameter: expectedPolicy.Metadata.NamespaceProperty,
                        cancellationToken: timeoutSource.Token
                    );
                }

                this.m_policiesRequested[policyNamespacedName] = new RequestedPolicy() {
                    deleteRequested = false,
                    policy = createdPolicy,
                    expireAt = DateTime.Now.AddSeconds(this.m_settings.RequestExpirationSeconds)
                };
            }
            return oldPolicy != null ? PolicyState.Updated : PolicyState.Created;
        }

        //TODO: maybe add ownerReferences to policy?

        private V1NetworkPolicy CreateEgressPolicy(LinkResource link, V1Service fromService, V1Service toService)
        {
            //https://kubernetes.io/docs/reference/generated/kubernetes-api/v1.26/#networkpolicyspec-v1-networking-k8s-io

            V1NetworkPolicy result = new V1NetworkPolicy();
            result.Initialize();
            result.Metadata.Name = link.GetEgressPolicyName();
            result.Metadata.NamespaceProperty = link.GetEgressPolicyNamespace();
            result.Metadata.Labels = new Dictionary<string, string>() {
                { POLICY_LABEL, link.GetNamespacedName() },
                { K8S_MANAGED_BY_LABEL, GROUP }
            };

            result.Spec = new V1NetworkPolicySpec() {
                PolicyTypes = new List<string>() { "Egress" },
                PodSelector = new V1LabelSelector() {
                    MatchLabels = fromService.Spec.Selector
                },
                Egress = new List<V1NetworkPolicyEgressRule>() {
                    new V1NetworkPolicyEgressRule() {
                        To = new List<V1NetworkPolicyPeer>() {
                            new V1NetworkPolicyPeer() {
                                PodSelector = new V1LabelSelector() {
                                    MatchLabels = toService.Spec.Selector
                                },
                                NamespaceSelector = toService.Metadata.NamespaceProperty == result.Metadata.NamespaceProperty
                                    ? null
                                    : new V1LabelSelector() {
                                        MatchLabels = new Dictionary<string, string>() {
                                            { K8S_NAME_LABEL, toService.Metadata.NamespaceProperty } //see https://kubernetes.io/docs/concepts/services-networking/network-policies/#targeting-a-namespace-by-its-name
                                        }
                                    }
                            }
                        },
                        Ports = new List<V1NetworkPolicyPort>() {
                            new V1NetworkPolicyPort() {
                                Port = link.Spec.to.port,
                                Protocol = link.Spec.to.protocol
                            }
                        }
                    }
                }
            };
            result.Validate();
            return result;
        }

        private V1NetworkPolicy CreateIngressPolicy(LinkResource link, V1Service fromService, V1Service toService)
        {
            //https://kubernetes.io/docs/reference/generated/kubernetes-api/v1.26/#networkpolicyspec-v1-networking-k8s-io

            V1NetworkPolicy result = new V1NetworkPolicy();
            result.Initialize();
            result.Metadata.Name = link.GetIngressPolicyName();
            result.Metadata.NamespaceProperty = link.GetIngressPolicyNamespace();
            result.Metadata.Labels = new Dictionary<string, string>() {
                { POLICY_LABEL, link.GetNamespacedName() },
                { K8S_MANAGED_BY_LABEL, GROUP }
            };

            result.Spec = new V1NetworkPolicySpec() {
                PolicyTypes = new List<string>() { "Ingress" },
                PodSelector = new V1LabelSelector() {
                    MatchLabels = toService.Spec.Selector
                },
                Ingress = new List<V1NetworkPolicyIngressRule>() {
                    new V1NetworkPolicyIngressRule() {
                        FromProperty = new List<V1NetworkPolicyPeer>() {
                            new V1NetworkPolicyPeer() {
                                PodSelector = new V1LabelSelector() {
                                    MatchLabels = fromService.Spec.Selector
                                },
                                NamespaceSelector = fromService.Metadata.NamespaceProperty == result.Metadata.NamespaceProperty
                                    ? null
                                    : new V1LabelSelector() {
                                        MatchLabels = new Dictionary<string, string>() {
                                            { K8S_NAME_LABEL, fromService.Metadata.NamespaceProperty } //see https://kubernetes.io/docs/concepts/services-networking/network-policies/#targeting-a-namespace-by-its-name
                                        }
                                    }
                            }
                        },
                        Ports = new List<V1NetworkPolicyPort>() {
                            new V1NetworkPolicyPort() {
                                Port = link.Spec.to.port,
                                Protocol = link.Spec.to.protocol
                            }
                        }
                    }
                }
            };
            result.Validate();
            return result;
        }

        public async ValueTask DisposeAsync()
        {
            this.m_tasksStopTokenSource.Cancel();

            try
            {
                await this.m_bgTasks;
            }
            catch (OperationCanceledException)
            {
                //ignore this
            }
            catch (Exception)
            {
                //and probably this
            }

            try
            {
                await this.m_catchBgTasksErrorTask;
            }
            catch (Exception)
            {
                //nothing to do
            }

            this.m_client.Dispose();
            this.m_tasksStopTokenSource.Dispose();
            this.m_stopTokenSource.Dispose();
        }

        private sealed class WatcherCollection : IDisposable
        {
            private readonly IDisposable[] m_watchers;

            internal WatcherCollection(params IDisposable[] watchers)
            {
                this.m_watchers = watchers;
            }

            public void Dispose()
            {
                foreach (IDisposable watcher in this.m_watchers)
                {
                    watcher.Dispose();
                }
            }
        }

        private sealed record EventWrapper(WatchEventType eventType, IMetadata<V1ObjectMeta> obj)
        {
        }

        private sealed class RequestedPolicy
        {
            internal V1NetworkPolicy policy { get; set; } = default!;
            internal bool deleteRequested { get; set; }
            internal DateTime expireAt { get; set; }
        }

        internal enum PolicyState
        {
            Consistent,
            Created,
            Updated,
            Removed
        }

        internal enum ServiceState
        {
            Ok,
            NotFound,
            NoSelector
        }
    }
}
