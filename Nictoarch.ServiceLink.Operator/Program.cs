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
using Nictoarch.ServiceLink.Operator.Resources;

namespace Nictoarch.ServiceLink.Operator
{
    internal sealed class Program
    {
        private static Logger s_logger = LogManager.GetCurrentClassLogger();

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
    }
}

