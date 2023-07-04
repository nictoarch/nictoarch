using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using IdentityModel.OidcClient;
using Jsonata.Net.Native;
using Jsonata.Net.Native.Json;
using k8s;
using k8s.Models;
using Nictoarch.Modelling.Core.Elements;
using Nictoarch.Modelling.K8s.Spec;

namespace Nictoarch.Modelling.K8s
{
    public sealed class K8sModelProviderStub: IDisposable
    {
        public static KubernetesClientConfiguration GetDefaultConfig(TimeSpan? httpClientTimeout = null)
        {
            KubernetesClientConfiguration config;
            if (KubernetesClientConfiguration.IsInCluster())
            {
                config = KubernetesClientConfiguration.InClusterConfig();
            }
            else
            {
                config = KubernetesClientConfiguration.BuildConfigFromConfigFile();
            }
            if (httpClientTimeout != null)
            {
                config.HttpClientTimeout = httpClientTimeout.Value;
            }
            return config;
        }

        private readonly K8sClient m_client;

        public K8sModelProviderStub(TimeSpan? httpClientTimeout = null)
            : this(GetDefaultConfig(httpClientTimeout))
        {

        }

        public K8sModelProviderStub(KubernetesClientConfiguration config)
        {
            this.m_client = new K8sClient(config);
        }

        public void Dispose()
        {
            this.m_client.Dispose();
        }

        /*
        public async Task<Model> GetModelAsync(ModelSpec spec, CancellationToken cancellationToken = default)
        {
            List<Entity> entities = new List<Entity>();
            if (spec.entities != null)
            {
                foreach (ModelSpec.EntitySelector selector in spec.entities)
                {
                    await this.GetEntities(selector, entities, cancellationToken);
                    cancellationToken.ThrowIfCancellationRequested();
                }
            }

            //TODO: links
            return new Model(spec.model_name, entities, new List<Link>());
        }
        */

        private async Task GetEntities(EntitySelector entitySelector, List<Entity> results, CancellationToken cancellationToken)
        {
            IReadOnlyList<JToken> resources = await this.GetResources(entitySelector, cancellationToken);
            foreach (JToken resource in resources)
            {
                Entity entity = this.K8sToEntity(resource, entitySelector);
                results.Add(entity);
                cancellationToken.ThrowIfCancellationRequested();
            }
        }

        private Task<IReadOnlyList<JToken>> GetResources(SelectorBase selector, CancellationToken cancellationToken)
        {
            return this.m_client.GetResources(
                apiGroup: selector.api_group, 
                resourceKind: selector.resource_kind.ToLowerInvariant(), 
                @namespace: selector.@namespace, 
                labelSelector: selector.label_query, 
                cancellationToken: cancellationToken
            );
        }

        private Entity K8sToEntity(JToken resource, EntitySelector entitySelector)
        {
            string domainId = this.EvaluateValueExpression(resource, entitySelector.domainIdQuery, nameof(entitySelector.domain_id_expr), entitySelector.domain_id_expr);
            string semanticId = this.EvaluateValueExpression(resource, entitySelector.semanticIdQuery, nameof(entitySelector.semantic_id_expr), entitySelector.semantic_id_expr);
            string displayName = this.EvaluateValueExpression(resource, entitySelector.displayNameQuery, nameof(entitySelector.display_name_expr), entitySelector.display_name_expr);

            Entity entity = new Entity(
                type: entitySelector.entity_type,
                domainId: domainId,
                semanticId: semanticId,
                displayName: displayName
            );

            return entity;
        }

        private string EvaluateValueExpression(JToken objectTree, JsonataQuery query, string expressionName, string expressionValue)
        {
            JToken result;
            try
            {
                result = query.Eval(objectTree);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to execute expression query '{expressionName}' ('{expressionValue}'): {ex.Message}", ex);
            }

            if (result.Type == JTokenType.String)
            {
                return (string)result;
            }
            else
            {
                return result.ToFlatString();
            }
        }
    }
}