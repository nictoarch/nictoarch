using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Nictoarch.Modelling.Core.Elements;
using NLog;

namespace Nictoarch.Modelling.Core
{
    public sealed class ModelProviderRegistry
    {
        private readonly Logger m_logger = LogManager.GetCurrentClassLogger();
        private readonly Dictionary<string, EntityProvider> m_entityProviders = new Dictionary<string, EntityProvider>();

        public IEnumerable<string> EntityProviderNames => this.m_entityProviders.Keys;

        public ModelProviderRegistry()
        {
            this.LoadProviders();
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

        private void LoadProviders()
        {
            List<string> fileNames = this.EnumeratePluginAssemblies();

            foreach (string dllName in fileNames)
            {
                this.LoadPluginsFromAssembly(dllName);
            }
        }

        private void LoadPluginsFromAssembly(string dllName)
        {
            Assembly pluginAssembly = Assembly.LoadFrom(dllName);
            AssemblyName assemblyName = new AssemblyName(pluginAssembly.FullName!);
            this.m_logger.Trace($"Loading providers from {assemblyName.Name} v{assemblyName.Version}");

            Type openEntityProviderType = typeof(IEntityProvider<>);

            foreach (Type classType in pluginAssembly.GetExportedTypes().Where(t => t.IsClass && !t.IsAbstract))
            {
                foreach (Type interfaceType in classType.GetInterfaces().Where(it => it.IsGenericType))
                {
                    if (openEntityProviderType.IsAssignableFrom(interfaceType.GetGenericTypeDefinition()))
                    {
                        Type specType = interfaceType.GetGenericArguments()[0];
                        EntityProvider provider = new EntityProvider(classType, specType);
                        this.m_entityProviders.Add(provider.Name, provider);
                        this.m_logger.Trace($"Added entity provider '{provider.Name}' from {classType.Name}");
                    }
                }
            }
        }

        internal bool GetEntityProvider(string name, [NotNullWhen(true)] out EntityProvider? provider)
        {
            return this.m_entityProviders.TryGetValue(name, out provider);
        }

        internal sealed class EntityProvider
        {
            private readonly IProviderBase m_providerInstance;
            private readonly MethodInfo m_getEntitiesMethod;

            internal string Name => this.m_providerInstance.Name;
            internal Type SpecType { get; }
            

            internal EntityProvider(Type providerType, Type specType)
            {
                this.SpecType = specType;
                this.m_providerInstance = (IProviderBase)Activator.CreateInstance(providerType)!;
                this.m_getEntitiesMethod = typeof(IEntityProvider<>)
                    .MakeGenericType(this.SpecType)
                    .GetMethod(nameof(IEntityProvider<object>.GetEntitiesAsync))!;
            }

            internal Task<List<Entity>> GetEntitiesAsync(object spec, CancellationToken cancellationToken)
            {
                if (!this.SpecType.IsAssignableFrom(spec.GetType())) 
                { 
                    throw new Exception($"Bad spec type ({spec.GetType()}) specified for Entity provider {this.Name}, expected {this.SpecType}");
                }

                return (Task<List<Entity>>)this.m_getEntitiesMethod.Invoke(this.m_providerInstance, new object[] { spec, cancellationToken })!;
            }
        }
    }
}
