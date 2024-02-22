using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Jsonata.Net.Native.Json;
using Nictoarch.Modelling.Core.BuiltinSources.Combined;
using Nictoarch.Modelling.Core.Elements;
using Nictoarch.Modelling.Core.Spec;
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
                Assembly pluginAssembly = Assembly.LoadFrom(dllName);
                this.LoadPluginsFromAssembly(pluginAssembly);
            }
        }

        private void LoadPluginsFromAssembly(Assembly pluginAssembly)
        {
            AssemblyName assemblyName = new AssemblyName(pluginAssembly.FullName!);
            this.m_logger.Trace($"Loading providers from {assemblyName.Name} v{assemblyName.Version}");

            Type openFactoryType = typeof(ISourceFactory<,,>);

            foreach (Type factoryClassType in pluginAssembly.GetExportedTypes().Where(t => t.IsClass && !t.IsAbstract))
            {
                foreach (Type interfaceType in factoryClassType.GetInterfaces().Where(it => it.IsGenericType))
                {
                    if (openFactoryType.IsAssignableFrom(interfaceType.GetGenericTypeDefinition()))
                    {
                        Type[] args = interfaceType.GetGenericArguments();
                        Type configType = args[0];
                        Type sourceType = args[1];
                        Type extractType = args[2];
                        SourceFactoryWrapper wrapper = new SourceFactoryWrapper(factoryClassType, configType, sourceType, extractType, this);
                        this.m_factoryWrappers.Add(wrapper.Name, wrapper);
                        this.m_logger.Trace($"Added source factory '{wrapper.Name}' from {factoryClassType.Name}");
                    }
                }
            }
        }

        internal IEnumerable<ITypeDiscriminator> GetYamlTypeDiscriminators()
        {

            //mapping for data.source.type field
            Dictionary<string, Type> sourceTypeMapping = this.m_factoryWrappers.Values.ToDictionary(f => f.Name, f => f.ConfigType);

            ITypeDiscriminator configTypeDiscriminator = new StrictKeyValueTypeDiscriminator(
                baseType: typeof(SourceConfigBase),
                targetKey: nameof(SourceConfigBase.type),
                typeMapping: sourceTypeMapping
            );

            return this.m_factoryWrappers.Values
                .SelectMany(w => w.GetYamlTypeDiscriminators())
                .Append(configTypeDiscriminator);
        }

        public bool GetProviderFactory(string name, [NotNullWhen(true)] out SourceFactoryWrapper? factory)
        {
            return this.m_factoryWrappers.TryGetValue(name, out factory);
        }

        internal bool GetProviderByConfigType(Type configType, [NotNullWhen(true)] out SourceFactoryWrapper? factory)
        {
            factory = this.m_factoryWrappers.Values.FirstOrDefault(w => w.ConfigType == configType);
            return factory != null;
        }

        public sealed class SourceFactoryWrapper
        {
            private readonly ISourceFactory m_factoryInstance;
            private readonly MethodInfo m_getSourceMethod;
            private readonly MethodInfo m_extractDataMethod;

            internal string Name => this.m_factoryInstance.Name;
            internal Type ConfigType { get; }
            internal Type SourceType { get; }
            internal Type ExtractType { get; }

            internal SourceFactoryWrapper(Type providerType, Type configType, Type sourceType, Type extractType, SourceRegistry registry)
            {
                this.ConfigType = configType;
                this.SourceType = sourceType;
                this.ExtractType = extractType;

                if (providerType == typeof(CombinedSourceFactory))
                {
                    //special constructor
                    this.m_factoryInstance = (ISourceFactory)Activator.CreateInstance(providerType, new object[] { registry })!;
                }
                else
                { 
                    this.m_factoryInstance = (ISourceFactory)Activator.CreateInstance(providerType)!;
                }

                this.m_getSourceMethod = typeof(ISourceFactory<,,>)
                    .MakeGenericType(this.ConfigType, this.SourceType, this.ExtractType)
                    .GetMethod(nameof(ISourceFactory<SourceConfigBase, ISource<ExtractConfigBase>, ExtractConfigBase>.GetSource))!;

                this.m_extractDataMethod = typeof(ISource<>)
                    .MakeGenericType(this.ExtractType)
                    .GetMethod(nameof(ISource<ExtractConfigBase>.Extract))!;
            }

            internal IEnumerable<ITypeDiscriminator> GetYamlTypeDiscriminators()
            {
                return this.m_factoryInstance.GetYamlTypeDiscriminators();
            }

            public Task<ISource> GetSource(SourceConfigBase sourceConfig, CancellationToken cancellationToken)
            {
                if (!this.ConfigType.IsAssignableFrom(sourceConfig.GetType())) 
                {
                    throw new Exception($"Should not happen! Factory {this.Name} expects source config of type {this.ConfigType.Name}, but was provided with {sourceConfig.GetType().Name}");
                }
                object result = this.m_getSourceMethod.Invoke(this.m_factoryInstance, new object[] { sourceConfig, cancellationToken })!;
                return (Task<ISource>)result;
            }

            public Task<JToken> Extract(ISource source, ExtractConfigBase extractConfig, CancellationToken cancellationToken)
            {
                if (!this.ExtractType.IsAssignableFrom(extractConfig.GetType()))
                {
                    throw new Exception($"Should not happen! Factory {this.Name} expects extract config of type {this.ExtractType.Name}, but was provided with {extractConfig.GetType().Name}");
                }
                return (Task<JToken>)this.m_extractDataMethod.Invoke(source, new object[] { extractConfig, cancellationToken })!;
            }

        }
    }
}

