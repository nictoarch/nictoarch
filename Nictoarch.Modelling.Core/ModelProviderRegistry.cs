﻿using System;
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
using YamlDotNet.Serialization;

namespace Nictoarch.Modelling.Core
{
    public sealed class ModelProviderRegistry
    {
        private readonly Logger m_logger = LogManager.GetCurrentClassLogger();
        private readonly Dictionary<string, ModelProviderFactory> m_providerFactories = new Dictionary<string, ModelProviderFactory>();

        public IEnumerable<string> ProviderNames => this.m_providerFactories.Keys;

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

            Type openFactoryType = typeof(IModelProviderFactory<,,>);

            foreach (Type factoryClassType in pluginAssembly.GetExportedTypes().Where(t => t.IsClass && !t.IsAbstract))
            {
                foreach (Type interfaceType in factoryClassType.GetInterfaces().Where(it => it.IsGenericType))
                {
                    if (openFactoryType.IsAssignableFrom(interfaceType.GetGenericTypeDefinition()))
                    {
                        Type[] args = interfaceType.GetGenericArguments();
                        Type configType = args[0];
                        Type entityConfigType = args[1];
                        Type validationConfigType = args[2];
                        ModelProviderFactory factory = new ModelProviderFactory(factoryClassType, configType, entityConfigType, validationConfigType);
                        this.m_providerFactories.Add(factory.Name, factory);
                        this.m_logger.Trace($"Added provider '{factory.Name}' from {factoryClassType.Name}");
                    }
                }
            }
        }

        internal void ConfigureYamlDeserialzier(DeserializerBuilder builder)
        {
            foreach (ModelProviderFactory factory in this.m_providerFactories.Values)
            {
                factory.ConfigureYamlDeserialzier(builder);
            }
        }

        internal bool GetProviderFactory(string name, [NotNullWhen(true)] out ModelProviderFactory? factory)
        {
            return this.m_providerFactories.TryGetValue(name, out factory);
        }

        internal sealed class ModelProviderFactory
        {
            private readonly IModelProviderFactory m_factoryInstance;
            private readonly MethodInfo m_getProviderMethod;
            private readonly MethodInfo m_getEntitesMethod;
            private readonly MethodInfo m_getInvalidObjectsMethod;

            internal string Name => this.m_factoryInstance.Name;
            internal Type ConfigType { get; }
            internal Type EntityConfigType { get; }
            internal Type ValidationConfigType { get; }

            internal ModelProviderFactory(Type providerType, Type configType, Type entityConfigType, Type validationConfigType)
            {
                this.ConfigType = configType;
                this.EntityConfigType = entityConfigType;
                this.ValidationConfigType = validationConfigType;

                this.m_factoryInstance = (IModelProviderFactory)Activator.CreateInstance(providerType)!;

                this.m_getProviderMethod = typeof(IModelProviderFactory<,,>)
                    .MakeGenericType(this.ConfigType, this.EntityConfigType, this.ValidationConfigType)
                    .GetMethod(nameof(IModelProviderFactory<object, object, object>.GetProviderAsync))!;
                this.m_getEntitesMethod = typeof(IModelProvider<,>)
                    .MakeGenericType(this.EntityConfigType, this.ValidationConfigType)
                    .GetMethod(nameof(IModelProvider<object, object>.GetEntitiesAsync))!;
                this.m_getInvalidObjectsMethod = typeof(IModelProvider<,>)
                    .MakeGenericType(this.EntityConfigType, this.ValidationConfigType)
                    .GetMethod(nameof(IModelProvider<object, object>.GetInvalidObjectsAsync))!;
            }

            internal void ConfigureYamlDeserialzier(DeserializerBuilder builder)
            {
                this.m_factoryInstance.ConfigureYamlDeserialzier(builder);
            }

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
        }
    }
}

