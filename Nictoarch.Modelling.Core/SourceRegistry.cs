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
using Nictoarch.Modelling.Core.Yaml;
using NLog;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.BufferedDeserialization;
using YamlDotNet.Serialization.BufferedDeserialization.TypeDiscriminators;

namespace Nictoarch.Modelling.Core
{
    public sealed class SourceRegistry
    {
        private readonly Logger m_logger = LogManager.GetCurrentClassLogger();
        private readonly Dictionary<string, SourceFactoryWrapper> m_factoryWrappers = new Dictionary<string, SourceFactoryWrapper>();

        public IEnumerable<string> ProviderNames => this.m_factoryWrappers.Keys;

        public SourceRegistry()
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

            Type openFactoryType = typeof(ISourceFactory<,>);

            foreach (Type factoryClassType in pluginAssembly.GetExportedTypes().Where(t => t.IsClass && !t.IsAbstract))
            {
                foreach (Type interfaceType in factoryClassType.GetInterfaces().Where(it => it.IsGenericType))
                {
                    if (openFactoryType.IsAssignableFrom(interfaceType.GetGenericTypeDefinition()))
                    {
                        Type[] args = interfaceType.GetGenericArguments();
                        Type configType = args[0];
                        Type extractType = args[1];
                        SourceFactoryWrapper wrapper = new SourceFactoryWrapper(factoryClassType, configType, extractType);
                        this.m_factoryWrappers.Add(wrapper.Name, wrapper);
                        this.m_logger.Trace($"Added sourcce factory '{wrapper.Name}' from {factoryClassType.Name}");
                    }
                }
            }
        }

        internal IEnumerable<ITypeDiscriminator> GetYamlTypeDiscriminators()
        {

            //mapping for data.source.type field
            Dictionary<string, Type> sourceTypeMapping = this.m_factoryWrappers.Values.ToDictionary(f => f.Name, f => f.ConfigType);

            ITypeDiscriminator configTypeDiscriminator = new StrictKeyValueTypeDiscriminator(
                baseType: typeof(ModelSpec.SourceConfigBase),
                targetKey: nameof(ModelSpec.SourceConfigBase.type),
                typeMapping: sourceTypeMapping
            );

            return this.m_factoryWrappers.Values
                .SelectMany(w => w.GetYamlTypeDiscriminators())
                .Append(configTypeDiscriminator);
        }

        internal bool GetProviderFactory(string name, [NotNullWhen(true)] out SourceFactoryWrapper? factory)
        {
            return this.m_factoryWrappers.TryGetValue(name, out factory);
        }

        internal bool GetProviderByConfigType(Type configType, [NotNullWhen(true)] out SourceFactoryWrapper? factory)
        {
            factory = this.m_factoryWrappers.Values.FirstOrDefault(w => w.ConfigType == configType);
            return factory != null;
        }

        internal sealed class SourceFactoryWrapper
        {
            private readonly ISourceFactory m_factoryInstance;
            /*
            private readonly MethodInfo m_getProviderMethod;
            private readonly MethodInfo m_getEntitesMethod;
            private readonly MethodInfo m_getInvalidObjectsMethod;
            */

            internal string Name => this.m_factoryInstance.Name;
            internal Type ConfigType { get; }
            internal Type ExtractType { get; }

            internal SourceFactoryWrapper(Type providerType, Type configType, Type extractType)
            {
                this.ConfigType = configType;
                this.ExtractType = extractType;

                this.m_factoryInstance = (ISourceFactory)Activator.CreateInstance(providerType)!;

                /*
                this.m_getProviderMethod = typeof(IModelProviderFactory<,,>)
                    .MakeGenericType(this.ConfigType, this.EntityConfigType, this.ValidationConfigType)
                    .GetMethod(nameof(IModelProviderFactory<object, object, object>.GetProviderAsync))!;
                this.m_getEntitesMethod = typeof(IModelProvider<,>)
                    .MakeGenericType(this.EntityConfigType, this.ValidationConfigType)
                    .GetMethod(nameof(IModelProvider<object, object>.GetEntitiesAsync))!;
                this.m_getInvalidObjectsMethod = typeof(IModelProvider<,>)
                    .MakeGenericType(this.EntityConfigType, this.ValidationConfigType)
                    .GetMethod(nameof(IModelProvider<object, object>.GetInvalidObjectsAsync))!;
                */
            }

            internal IEnumerable<ITypeDiscriminator> GetYamlTypeDiscriminators()
            {
                return this.m_factoryInstance.GetYamlTypeDiscriminators();
            }

            /*
            internal Task<IModelProvider> GetProviderAsync(object config, CancellationToken cancellationToken)
            {
                if (!this.ConfigType.IsAssignableFrom(config.GetType())) 
                { 
                    throw new Exception($"Bad config type ({config.GetType()}) specified for Model provider {this.Name}, expected {this.ConfigType}");
                }

                return (Task<IModelProvider>)this.m_getProviderMethod.Invoke(this.m_factoryInstance, new object[] { config, cancellationToken })!;
            }

            internal Task<List<Entity>> GetEntitiesAsync(IModelProvider provider, object config, CancellationToken cancellationToken)
            {
                if (!this.EntityConfigType.IsAssignableFrom(config.GetType()))
                {
                    throw new Exception($"Bad Entity config type ({config.GetType()}) specified for Model provider {this.Name}, expected {this.EntityConfigType}");
                }

                return (Task<List<Entity>>)this.m_getEntitesMethod.Invoke(provider, new object[] { config, cancellationToken })!;
            }

            internal Task<List<object>> GetInvalidObjactsAsync(IModelProvider provider, object config, CancellationToken cancellationToken)
            {
                if (!this.ValidationConfigType.IsAssignableFrom(config.GetType()))
                {
                    throw new Exception($"Bad Validation config type ({config.GetType()}) specified for Model provider {this.Name}, expected {this.ValidationConfigType}");
                }

                return (Task<List<object>>)this.m_getInvalidObjectsMethod.Invoke(provider, new object[] { config, cancellationToken })!;
            }
            */
        }
    }
}

