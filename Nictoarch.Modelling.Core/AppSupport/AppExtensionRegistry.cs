using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Nictoarch.Modelling.Core.Elements;
using NLog;

namespace Nictoarch.Modelling.Core.AppSupport
{
    public sealed class AppExtensionsRegistry
    {
        private readonly Logger m_logger = LogManager.GetCurrentClassLogger();
        private readonly List<IAppExtension> m_extensions = new List<IAppExtension>();

        public AppExtensionsRegistry()
        {
            LoadExtensions();
        }

        public void PopulateRootCommand(Command root)
        { 
            foreach (IAppExtension extension in this.m_extensions)
            {
                Command subcommand = new Command(extension.Name, extension.Description);
                foreach (Command extCommand in extension.GetCommands())
                {
                    subcommand.AddCommand(extCommand);
                }
                root.AddCommand(subcommand);
            }
        }

        private List<string> EnumeratePluginAssemblies()
        {
            List<string> result = new List<string>();
            string path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
            foreach (string dllName in Directory.EnumerateFiles(path, $"{nameof(Nictoarch)}.{nameof(Modelling)}.*.dll"))
            {
                result.Add(dllName);
            }
            return result;
        }

        private void LoadExtensions()
        {
            List<string> fileNames = EnumeratePluginAssemblies();

            foreach (string dllName in fileNames)
            {
                LoadExtensionFromAssembly(dllName);
            }
        }

        private void LoadExtensionFromAssembly(string dllName)
        {
            Assembly pluginAssembly = Assembly.LoadFrom(dllName);
            AssemblyName assemblyName = new AssemblyName(pluginAssembly.FullName!);
            this.m_logger.Trace($"Loading app extensions from {assemblyName.Name} v{assemblyName.Version}");

            foreach (Type extensionType in pluginAssembly.GetExportedTypes()
                .Where(t => t.IsClass && !t.IsAbstract && typeof(IAppExtension).IsAssignableFrom(t))
            )
            {
                IAppExtension extension = (IAppExtension)Activator.CreateInstance(extensionType)!;
                this.m_logger.Trace($"Added extension '{extension.Name}' from {extensionType.Name}");
                this.m_extensions.Add(extension);
            }
        }
    }
}

