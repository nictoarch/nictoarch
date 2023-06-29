using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Jsonata.Net.Native;
using Jsonata.Net.Native.Json;
using k8s;
using k8s.Models;
using Nictoarch.Modelling.Core.Elements;

namespace Nictoarch.Modelling.K8s
{
    public sealed class K8sModelElementsProvider
    {
        private readonly IKubernetes m_client;

        public K8sModelElementsProvider(IKubernetes client)
        {
            this.m_client = client;
        }

        public async Task<Model> GetModelAsync(string modelName, Spec spec, CancellationToken cancellationToken = default)
        {
            List<Entity> entities = new List<Entity>();
            if (spec.entities != null)
            {
                foreach (Spec.EntitySelector selector in spec.entities)
                {
                    await this.GetEntities(selector, entities, cancellationToken);

                    if (cancellationToken.IsCancellationRequested)
                    {
                        throw new OperationCanceledException();
                    }
                }
            }

            return new Model(modelName, entities, links);
        }

        private async Task GetEntities(Spec.EntitySelector entitySelector, List<Entity> results, CancellationToken cancellationToken)
        {
            switch (entitySelector.resource_kind)
            {
            case Spec.EntitySelector.ResourceKind.Service:
                await this.GetEntitiesFromServices(entitySelector, results, cancellationToken);
                break;
            default:
                throw new Exception("Unexpected resource kind for entity selector: " + entitySelector.resource_kind);
            }
        }

        private async Task GetEntitiesFromServices(Spec.EntitySelector entitySelector, List<Entity> results, CancellationToken cancellationToken)
        {
            V1ServiceList services;

            if (entitySelector.@namespace == "*")
            {
                services = await this.m_client.CoreV1.ListServiceForAllNamespacesAsync(
                    labelSelector: entitySelector.label_query,
                    cancellationToken: cancellationToken
                );
            }
            else
            {
                services = await this.m_client.CoreV1.ListNamespacedServiceAsync(
                    namespaceParameter: entitySelector.@namespace,
                    labelSelector: entitySelector.label_query,
                    cancellationToken: cancellationToken
                );
            }

            foreach (V1Service service in services.Items)
            {
                results.Add(this.K8sToEntity(service, entitySelector));
            }
        }

        private Entity K8sToEntity(IKubernetesObject obj, Spec.EntitySelector entitySelector)
        {
            JToken objectTree = JToken.FromObject(obj);

            string domainId = this.EvaluateValueExpression(objectTree, entitySelector.domainIdQuery, nameof(entitySelector.domain_id_expr), entitySelector.domain_id_expr);
            string semanticId = this.EvaluateValueExpression(objectTree, entitySelector.semanticIdQuery, nameof(entitySelector.semantic_id_expr), entitySelector.semantic_id_expr);
            string displayName = this.EvaluateValueExpression(objectTree, entitySelector.displayNameQuery, nameof(entitySelector.display_name_expr), entitySelector.display_name_expr);

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
            try
            {
                JToken result = query.Eval(objectTree);
                return result.ToFlatString();
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to exequse expression query '{expressionName}' ('{expressionValue}'): {ex.Message}", ex);
            }
        }
    }
}