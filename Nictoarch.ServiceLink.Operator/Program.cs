using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using k8s;
using k8s.Autorest;
using k8s.Models;
using Nictoarch.Common;
using NLog;

namespace Nictoarch.ServiceLink.Operator
{
    internal sealed class Program
    {
        private static Logger s_logger = LogManager.GetCurrentClassLogger();

        private static void OnServiceEvent(WatchEventType eventType, V1Service service)
        {
            s_logger.Trace($"Service event {eventType} for {service.Metadata.Name} (v {service.Metadata.ResourceVersion}, id {service.Metadata.Uid})");
        }

        private static void OnCustomObjectEvent(WatchEventType eventType, LinkResource customObj)
        {
            s_logger.Trace($"Obj event {eventType} for {customObj.Metadata.Name} (v {customObj.Metadata.ResourceVersion}, id {customObj.Metadata.Uid})");
        }

        public static async Task Main(string[] args)
        {
            await ProgramHelper.MainWrapperAsync(async () => {

                //TODO: load from file, add watcher
                Settings settings = new Settings();
                await using (Controller controller = new Controller(settings))
                {
                    using (await controller.InitAsync())
                    {
                        Console.CancelKeyPress += (sender, eventArgs) => {
                            s_logger.Trace("CtrlC!");
                            eventArgs.Cancel = true;
                            controller.Stop();
                        };

                        await controller.RunAsync();
                    }
                }
            });
        }

        public static async Task Main2(string[] args)
        {
            await ProgramHelper.MainWrapperAsync(async () => {

                string kubeConfFilePath = args[0];
                //ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls13;
                KubernetesClientConfiguration config = KubernetesClientConfiguration.BuildConfigFromConfigFile(kubeConfFilePath);
                s_logger.Trace($"Connecting to {config.CurrentContext}, {config.Host}, {config.Namespace}, {config.Username}");
                config.SkipTlsVerify = true;
                //config.DisableHttp2 = true;

                const string GROUP = "servicelink.io";
                const string VERSION = "v1";
                const string PLURAL = "links";
                const string NAMESPACE = "default";
                using (CancellationTokenSource stopTokenSource = new CancellationTokenSource())
                using (IKubernetes client = new Kubernetes(config))
                {
                    s_logger.Trace($"Connected");

                    V1ServiceList serviceList = await client.CoreV1.ListNamespacedServiceAsync(NAMESPACE);
                    Task<HttpOperationResponse<V1ServiceList>> serviceListWatchTask = client.CoreV1.ListNamespacedServiceWithHttpMessagesAsync(
                        NAMESPACE,
                        watch: true,
                        resourceVersion: serviceList!.Metadata.ResourceVersion,
                        cancellationToken: stopTokenSource.Token
                    );

                    CustomResourceList<LinkResource> customObjectList = await client.ListNamespacedCustomObjectAsync<CustomResourceList<LinkResource>>(
                        group: GROUP,
                        version: VERSION,
                        namespaceParameter: NAMESPACE,
                        plural: PLURAL
                    );
                    Task<HttpOperationResponse<object>> customObjectWatchTask = client.CustomObjects.ListNamespacedCustomObjectWithHttpMessagesAsync(
                        group: GROUP,
                        version: VERSION,
                        namespaceParameter: NAMESPACE,
                        plural: PLURAL,
                        watch: true,
                        resourceVersion: customObjectList!.Metadata.ResourceVersion,
                        cancellationToken: stopTokenSource.Token
                    );

                    using (Watcher<V1Service> serviceWatcher = serviceListWatchTask.Watch<V1Service, V1ServiceList>(onEvent: OnServiceEvent))
                    using (Watcher<LinkResource> objectWatcher = customObjectWatchTask.Watch<LinkResource, object>(onEvent: OnCustomObjectEvent))
                    {
                        s_logger.Trace($"Services:");
                        foreach (V1Service service in serviceList.Items.OrderBy(n => n.Metadata.Name))
                        {
                            s_logger.Trace($"\tservice: {service.Metadata.Name}, v {service.Metadata.ResourceVersion}, id {service.Metadata.Uid}");
                            s_logger.Trace("\t\ttype: " + service.Spec.Type);
                            foreach (V1ServicePort port in service.Spec.Ports)
                            {
                                s_logger.Trace($"\t\tport: {port.AppProtocol} ({port.Protocol}) {port.Name}: {port.Port} {port.NodePort} {port.TargetPort}");
                            }
                        }

                        s_logger.Trace($"Links:");
                        foreach (LinkResource res in customObjectList.Items)
                        {
                            s_logger.Trace($"\tres: {res.Metadata.Name}, v {res.Metadata.ResourceVersion}, id {res.Metadata.Uid}");
                            /*
                            res.Status = new CResourceStatus() {
                                state = "aaa!"
                            };
                            client.CustomObjects.ReplaceNamespacedCustomObjectStatus(
                                body: res,
                                group: GROUP,
                                version: VERSION,
                                namespaceParameter: ns.Metadata.Name,
                                plural: PLURAL,
                                name: res.Metadata.Name
                            );
                            */
                        }

                        s_logger.Trace("Waiting for events");


                        TaskCompletionSource ctrlc = new TaskCompletionSource();

                        Console.CancelKeyPress += (sender, eventArgs) => {
                            s_logger.Trace("CtrlC!");
                            eventArgs.Cancel = true;
                            ctrlc.SetResult();
                        };

                        s_logger.Trace("press ctrl + c to stop watching");
                        await ctrlc.Task;
                        s_logger.Trace("Exiting");
                        stopTokenSource.Cancel();
                    }
                    s_logger.Trace("Quit watcher");

                    try
                    {
                        await Task.WhenAll(serviceListWatchTask, customObjectWatchTask);
                    }
                    catch (OperationCanceledException)
                    {
                        //just skip it
                    }
                    catch (Exception ex)
                    {
                        //probably some aggregations
                        Console.WriteLine(ex);
                    }
                }
                s_logger.Trace("Quit kube client");
            });

            s_logger.Trace("Quit");
        }
    }
}

