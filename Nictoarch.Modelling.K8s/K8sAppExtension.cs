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
using Nictoarch.Modelling.K8s.Spec;
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
                Command listApisCommand = new Command("resources", "Lists available K8s resources");
                listApisCommand.AddAlias("res");

                Option<string?> configFileOption = new Option<string?>(new string[] { "--config", "-c" }, () => null, "Config file location");
                listApisCommand.Add(configFileOption);
                Option<bool> detailsOption = new Option<bool>(new string[] { "--details", "-d" }, () => false, "Output all details");
                listApisCommand.Add(detailsOption);

                listApisCommand.SetHandler(
                    this.ListApisCommand,
                    configFileOption, detailsOption
                );

                result.Add(listApisCommand);
            }

            return result;
        }

        private async Task ListApisCommand(string? configFileName, bool showDetails)
        {
            if (configFileName != null)
            {
                this.m_logger.Trace("Using config file at " + configFileName);
            }

            KubernetesClientConfiguration config = K8sClient.GetConfiguration(ProviderConfig.ConnectViaType.config_file, configFileName);
            using (K8sClient client = new K8sClient(config))
            {
                IReadOnlyList<ApiInfo> apis = await client.GetApiInfosCached(CancellationToken.None);
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
