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
using NLog;
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
        private readonly Logger m_logger = LogManager.GetCurrentClassLogger();

        //all keys are NamespacedNames, see K8sExtensions.GetNamespacedName()
        private readonly Dictionary<string, V1Service> m_servicesReported = new Dictionary<string, V1Service>();
        private readonly Dictionary<string, V1NetworkPolicy> m_policiesReported = new Dictionary<string, V1NetworkPolicy>();
        private readonly Dictionary<string, LinkResource> m_linksReported = new Dictionary<string, LinkResource>();

        private readonly Dictionary<string, RequestedPolicy> m_policiesRequested = new Dictionary<string, RequestedPolicy>();

        private readonly Channel<EventWrapper> m_channel = Channel.CreateUnbounded<EventWrapper>();

        private readonly CancellationTokenSource m_stopTokenSource = new CancellationTokenSource();
        private readonly IKubernetes m_client;

        internal Controller(Settings settings)
        {
            this.m_settings = settings;

            KubernetesClientConfiguration config;
            if (KubernetesClientConfiguration.IsInCluster())
            {
                this.m_logger.Trace("Running with in-cluster config");
                config = KubernetesClientConfiguration.InClusterConfig();
            }
            else
            {
                this.m_logger.Trace("Running with default config file from " + KubernetesClientConfiguration.KubeConfigDefaultLocation);
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
            this.m_logger.Trace("listing Services");
            V1ServiceList serviceList = await this.m_client.CoreV1.ListServiceForAllNamespacesAsync();
            Task<HttpOperationResponse<V1ServiceList>> serviceListWatchTask = this.m_client.CoreV1.ListServiceForAllNamespacesWithHttpMessagesAsync(
                watch: true,
                resourceVersion: serviceList!.Metadata.ResourceVersion
            );
            foreach (V1Service service in serviceList.Items)
            {
                this.m_servicesReported.Add(service.Metadata.Uid, service);
            }
            this.m_logger.Trace($"Got {this.m_servicesReported.Count} initial services");

            this.m_logger.Trace("listing NetworkPolicies");
            V1NetworkPolicyList policiesList = await this.m_client.NetworkingV1.ListNetworkPolicyForAllNamespacesAsync(
                labelSelector: POLICY_LABEL
            );
            Task<HttpOperationResponse<V1NetworkPolicyList>> policyListWatchTask = this.m_client.NetworkingV1.ListNetworkPolicyForAllNamespacesWithHttpMessagesAsync(
                labelSelector: POLICY_LABEL,
                watch: true,
                resourceVersion: policiesList!.Metadata.ResourceVersion
            );
            foreach (V1NetworkPolicy policy in policiesList.Items)
            {
                this.m_policiesReported.Add(policy.Metadata.Uid, policy);
            }
            this.m_logger.Trace($"Got {this.m_policiesReported.Count} initial managed network policies");

            this.m_logger.Trace("listing Link custom resources");
            LinkResourceList linksList = await this.m_client.CustomObjects.ListCustomObjectForAllNamespacesAsync<LinkResourceList>(
                group: GROUP,
                version: VERSION,
                plural: PLURAL
            );
            Task<HttpOperationResponse<LinkResourceList>> linkListWatchTask = this.m_client.CustomObjects.ListCustomObjectForAllNamespacesWithHttpMessagesAsync<LinkResourceList>(
                group: GROUP,
                version: VERSION,
                plural: PLURAL,
                watch: true,
                resourceVersion: linksList!.Metadata.ResourceVersion
            );
            foreach (LinkResource link in linksList.Items)
            {
                this.m_linksReported.Add(link.Metadata.Uid, link);
            }
            this.m_logger.Trace($"Got {this.m_linksReported.Count} initial links");

            this.m_logger.Trace("Starting watchers");

            return new WatcherCollection(
                serviceListWatchTask.Watch<V1Service, V1ServiceList>(onEvent: OnWatcherEvent, onError: OnWatcherError, onClosed: OnWatcherClosed),
                policyListWatchTask.Watch<V1NetworkPolicy, V1NetworkPolicyList>(onEvent: OnWatcherEvent, onError: OnWatcherError, onClosed: OnWatcherClosed),
                linkListWatchTask.Watch<LinkResource, LinkResourceList>(onEvent: OnWatcherEvent, onError: OnWatcherError, onClosed: OnWatcherClosed)
            );
        }

        private void TerminateOnError(Exception ex)
        {
            ProgramHelper.HandleFatalException(ex, ProgramHelper.ExceptionSource.MainAction_Exception);
        }

        private void OnWatcherClosed()
        {
            if (!this.m_stopTokenSource.IsCancellationRequested)
            {
                this.m_logger.Warn($"Got {nameof(OnWatcherClosed)}, terminating");
                this.TerminateOnError(new Exception(nameof(OnWatcherClosed)));
            }
            else
            {
                this.m_logger.Trace($"Got {nameof(OnWatcherClosed)} during termination");
            }
        }

        private void OnWatcherError(Exception ex)
        {
            if (!this.m_stopTokenSource.IsCancellationRequested)
            {
                this.m_logger.Warn(ex, $"Got {nameof(OnWatcherError)}: {ex.Message}, terminating");
                this.TerminateOnError(ex);
            }
            else
            {
                this.m_logger.Trace($"Got {nameof(OnWatcherError)}: {ex.Message} during termination");
            }
        }

        private void OnWatcherEvent<T>(WatchEventType eventType, T resource)
            where T : IKubernetesObject<V1ObjectMeta>
        {
            if (!this.m_channel.Writer.TryWrite(new EventWrapper(eventType, resource)))
            {
                if (!this.m_stopTokenSource.IsCancellationRequested)
                {
                    this.m_logger.Warn($"Failed to push watcher event ({eventType}: {resource?.Kind} {resource?.Metadata?.NamespaceProperty} {resource?.Metadata?.Name}) through the channel");
                    this.TerminateOnError(new Exception("Failed to push event"));
                }
            }
        }

        internal async Task RunAsync()
        {
            if (!this.m_stopTokenSource.IsCancellationRequested)
            {
                await this.Reconcile();
            }

            while (!this.m_stopTokenSource.IsCancellationRequested)
            {
                try
                {
                    await this.ReadEventsAsync();
                }
                catch (OperationCanceledException) when (this.m_stopTokenSource.IsCancellationRequested)
                {
                    break;
                }

                await this.Reconcile();
            }
        }

        private async Task ReadEventsAsync()
        {
            await this.m_channel.Reader.WaitToReadAsync(this.m_stopTokenSource.Token);
            //stopping would throw and interrupt

            int iteration = 0;
            while (iteration < this.m_settings.MaxBatchSize)
            {
                while (this.m_channel.Reader.TryRead(out EventWrapper? eventWrapper) && iteration < this.m_settings.MaxBatchSize)
                {
                    ++iteration;
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

            if (iteration >= this.m_settings.MaxBatchSize)
            {
                this.m_logger.Warn($"Iteration limit of {iteration} reached in {nameof(ReadEventsAsync)}, should not happen");
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
                throw new Exception($"Unexpected type {eventWrapper.obj.GetType().Name} ({eventWrapper.obj.Kind})");
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
            this.m_logger.Trace("Running reconcile step");

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
                ServiceState egressServiceState = this.GetService(link.GetEgressPolicyServiceNamespacedName(), link, out V1Service? egressService);
                ServiceState ingressServiceState = this.GetService(link.GetIngressPolicyServiceNamespacedName(), link, out V1Service? ingressService);
                
                PolicyState egressPolicyState;
                PolicyState ingressPolicyState;
                //only if both services exist and have selectors, we are able to construct a policy (for now)
                if (egressServiceState != ServiceState.Ok || ingressServiceState != ServiceState.Ok)
                {
                    //something not ok
                    egressPolicyState = await this.ProcessDeleteNetworkPolicyIfExistsAsync(egressPolicyNamespacedName, checkedPolicies);
                    ingressPolicyState = await this.ProcessDeleteNetworkPolicyIfExistsAsync(ingressPolicyNamespacedName, checkedPolicies);
                }
                else
                {
                    egressPolicyState = await this.ReconcileNetworkPolicyAsync(egressPolicyNamespacedName, checkedPolicies, this.CreateEgressPolicyObject(link, egressService!, ingressService!));
                    ingressPolicyState = await this.ReconcileNetworkPolicyAsync(ingressPolicyNamespacedName, checkedPolicies, this.CreateIngressPolicyObject(link, egressService!, ingressService!));
                }

                if (link.UpdateState(egressServiceState, ingressServiceState, egressPolicyState, ingressPolicyState))
                {
                    this.m_logger.Trace("Requesting status update for Link " + link.GetNamespacedName());
                    using (CancellationTokenSource timeoutSource = new CancellationTokenSource(this.m_settings.OperationTimeoutMs))
                    {
                        LinkResource result = await this.m_client.CustomObjects.ReplaceNamespacedCustomObjectStatusAsync<LinkResource>(
                            body: link,
                            group: GROUP,
                            version: VERSION,
                            namespaceParameter: link.Metadata.Name,
                            plural: PLURAL,
                            name: link.Metadata.Name
                        );
                        //here result is an updated link
                        //TODO: update resource version = update link in the m_reportedLinks or wait for it to come from server reports?
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
                await this.DeleteNetworkPolicyAsync(key, policy);
            }
        }

        private async Task DeleteNetworkPolicyAsync(string policyNamespacedName, V1NetworkPolicy policy)
        {
            this.m_logger.Trace("Requesting deletion of network policy " + policy.GetNamespacedName());
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

        private async Task<PolicyState> ProcessDeleteNetworkPolicyIfExistsAsync(string policyNamespacedName, HashSet<string> checkedPolicies)
        {
            if (!checkedPolicies.Add(policyNamespacedName))
            {
                throw new Exception("Checking same policy twice: " + policyNamespacedName);
            }

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
                await this.DeleteNetworkPolicyAsync(policyNamespacedName, policyToDelete);
            }

            return PolicyState.Removed;
        }

        private ServiceState GetService(string serviceNamespacedName, LinkResource link, out V1Service? service)
        {
            if (!this.m_servicesReported.TryGetValue(serviceNamespacedName, out service))
            {
                this.m_logger.Warn($"No service {serviceNamespacedName} found, requested by Link {link.GetNamespacedName()}");
                return ServiceState.NotFound;
            }
            else if (service.Spec.Selector == null || service.Spec.Selector.Count == 0)
            {
                this.m_logger.Warn($"Service {serviceNamespacedName} requested by Link {link.GetNamespacedName()} has no label selector for pods");
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
                    if (NetworkPolicyComparer.Compare(expectedPolicy, requestedPolicy.policy))
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
                if (NetworkPolicyComparer.Compare(expectedPolicy, reportedPolicy))
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
                    this.m_logger.Trace($"Replacing network policy {expectedPolicy.GetNamespacedName()}");
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
                    this.m_logger.Trace($"Creating network policy {expectedPolicy.GetNamespacedName()}");
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
        //TODO: allow adding additional labels and annotations to managed policies
        private V1NetworkPolicy CreateEgressPolicyObject(LinkResource link, V1Service fromService, V1Service toService)
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

        private V1NetworkPolicy CreateIngressPolicyObject(LinkResource link, V1Service fromService, V1Service toService)
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

        public ValueTask DisposeAsync()
        {
            this.m_client.Dispose();
            this.m_stopTokenSource.Dispose();
            return ValueTask.CompletedTask;
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

        private sealed class EventWrapper
        {
            internal readonly WatchEventType eventType;
            internal readonly IKubernetesObject<V1ObjectMeta> obj;

            internal EventWrapper(WatchEventType eventType, IKubernetesObject<V1ObjectMeta> obj)
            {
                this.eventType = eventType;
                this.obj = obj;
            }
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
