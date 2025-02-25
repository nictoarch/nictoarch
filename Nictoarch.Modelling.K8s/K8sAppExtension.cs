using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Jsonata.Net.Native.Json;
using k8s;
using Nictoarch.Modelling.Core.AppSupport;
using NLog;

namespace Nictoarch.Modelling.K8s
{
    public sealed class K8sAppExtension : IAppExtension
    {
        private readonly Logger m_logger = LogManager.GetCurrentClassLogger();

        string IAppExtension.Name => "k8s";
        string IAppExtension.Description => "Kubernetes-specific commands";

        List<Command> IAppExtension.GetCommands()
        {
            List<Command> result = new List<Command>();

            {
                Command listApisCommand = new Command("resource-types", "Lists available K8s resources");
                listApisCommand.AddAlias("res-types");
                listApisCommand.AddAlias("types");

                Option<string?> configFileOption = new Option<string?>(new string[] { "--config", "-c" }, () => null, "Config file location");
                Option<bool> detailsOption = new Option<bool>(new string[] { "--details", "-d" }, () => false, "Output all details");
                listApisCommand.Add(configFileOption);
                listApisCommand.Add(detailsOption);

                listApisCommand.SetHandler(
                    this.ListApisCommand,
                    configFileOption, detailsOption
                );

                result.Add(listApisCommand);
            }

            {
                Command listApisCommand = new Command("resource-get", "Get resources of a type");
                listApisCommand.AddAlias("res-get");
                listApisCommand.AddAlias("get");

                Argument<string> typeArg = new Argument<string>("type", "Type of resource to get");
                Option<string?> namespaceOption = new Option<string?>(new string[] { "--namespace", "--ns", "-n" }, () => null, "Namespace to get resources from");
                Option<string?> labelsOption = new Option<string?>(new string[] { "--labels", "-l" }, () => null, "Label selector query");
                Option<string?> configFileOption = new Option<string?>(new string[] { "--config", "-c" }, () => null, "Config file location");
                listApisCommand.Add(typeArg);
                listApisCommand.Add(namespaceOption);
                listApisCommand.Add(labelsOption);
                listApisCommand.Add(configFileOption);

                listApisCommand.SetHandler(
                    this.GetResourcesCommand,
                    typeArg, namespaceOption, labelsOption, configFileOption
                );

                result.Add(listApisCommand);
            }

            {
                Command getVersionCommand = new Command("cluster-version", "Get version of a cluster");
                getVersionCommand.AddAlias("version");
                Option<string?> configFileOption = new Option<string?>(new string[] { "--config", "-c" }, () => null, "Config file location");
                getVersionCommand.Add(configFileOption);

                getVersionCommand.SetHandler(
                    this.GetVersionCommand,
                    configFileOption
                );

                result.Add(getVersionCommand);
            }

            return result;
        }

        private async Task GetResourcesCommand(string resourceType, string? @namespace, string? labels, string? configFileName)
        {
            if (configFileName != null)
            {
                this.m_logger.Trace("Using config file at " + configFileName);
            }

            KubernetesClientConfiguration config = K8sClient.GetConfiguration(false, configFileName);
            using (K8sClient client = new K8sClient(config))
            {

                await client.InitAsync(CancellationToken.None);
                JArray resources = await client.GetResources(
                    apiGroup: null,
                    resourceKind: resourceType.ToLowerInvariant(),
                    @namespace: @namespace,
                    labelSelector: labels,
                    cancellationToken: CancellationToken.None
                );

                
                Console.WriteLine(resources.ToIndentedString());
            }
        }

        private async Task GetVersionCommand(string? configFileName)
        {
            if (configFileName != null)
            {
                this.m_logger.Trace("Using config file at " + configFileName);
            }

            KubernetesClientConfiguration config = K8sClient.GetConfiguration(false, configFileName);
            using (K8sClient client = new K8sClient(config))
            {
                JToken result = await client.GetVersion(CancellationToken.None);
                Console.WriteLine(result.ToIndentedString());
            }
        }

        private async Task ListApisCommand(string? configFileName, bool showDetails)
        {
            if (configFileName != null)
            {
                this.m_logger.Trace("Using config file at " + configFileName);
            }

            KubernetesClientConfiguration config = K8sClient.GetConfiguration(false, configFileName);
            using (K8sClient client = new K8sClient(config))
            {
                await client.InitAsync(CancellationToken.None);
                IReadOnlyList<ApiInfo> apis = client.ApiInfos;
                if (showDetails)
                {
                    JToken result = JToken.FromObject(apis);
                    Console.WriteLine(result.ToIndentedString());
                }
                else
                {
                    foreach (ApiInfo api in apis)
                    {
                        Console.WriteLine(api.resource_singular);
                    }
                }
            }
        }
    }
}
