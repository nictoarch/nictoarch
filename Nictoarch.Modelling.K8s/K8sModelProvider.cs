using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Jsonata.Net.Native.Json;
using Jsonata.Net.Native;
using k8s;
using Nictoarch.Modelling.Core;
using Nictoarch.Modelling.Core.Elements;
using Nictoarch.Modelling.K8s.Spec;

namespace Nictoarch.Modelling.K8s
{
    public sealed class K8sModelProvider : IModelProvider<EntitySelector, SelectorBase>
    {
        private readonly K8sClient m_client;

        internal K8sModelProvider(K8sClient client)
        {
            this.m_client = client;
        }

        void IDisposable.Dispose()
        {
            this.m_client.Dispose();
        }

        async Task<List<Entity>> IModelProvider<EntitySelector, SelectorBase>.GetEntitiesAsync(EntitySelector entitySelector, CancellationToken cancellationToken)
        {
            IReadOnlyList<JToken> resources = await this.GetResources(entitySelector, cancellationToken);
            List<Entity> results = new List<Entity>(resources.Count);
            foreach (JToken resource in resources)
            {
                Entity entity = this.K8sToEntity(resource, entitySelector);
                results.Add(entity);
                cancellationToken.ThrowIfCancellationRequested();
            }
            return results;
        }

        async Task<List<object>> IModelProvider<EntitySelector, SelectorBase>.GetInvalidObjectsAsync(SelectorBase objectSelector, CancellationToken cancellationToken)
        {
            IReadOnlyList<JToken> resources = await this.GetResources(objectSelector, cancellationToken);
            List<object> results = new List<object>(resources.Count);
            foreach (JToken resource in resources)
            {
                results.Add(resource);
                cancellationToken.ThrowIfCancellationRequested();
            }
            return results;
        }

        private async Task<IReadOnlyList<JToken>> GetResources(SelectorBase selector, CancellationToken cancellationToken)
        {
            IReadOnlyList<JToken> resources = await this.m_client.GetResources(
                apiGroup: selector.api_group,
                resourceKind: selector.resource_kind.ToLowerInvariant(),
                @namespace: selector.@namespace,
                labelSelector: selector.label_query,
                cancellationToken: cancellationToken
            );

            if (selector.filterQuery != null)
            {
                List<JToken> filtered = new List<JToken>(resources.Count);

                foreach (JToken resource in resources)
                {
                    try
                    {
                        JToken filterResult = selector.filterQuery.Eval(resource);
                        if (filterResult.Type == JTokenType.Null
                            || filterResult.Type == JTokenType.Undefined
                            || (filterResult.Type == JTokenType.Boolean && ((bool)filterResult == false))
                        )
                        {
                            continue; //filter failed
                        }
                        else
                        {
                            filtered.Add(resource);
                        }
                    }
                    catch (Exception ex)
                    {
                        throw new Exception($"Failed to execute filter on resource: {ex.Message}. Filter: '{selector.filter_expr}', resource: '{resource.ToFlatString()}'");
                    }
                }
                return filtered;
            }
            else
            {
                return resources;
            }
        }

        private Entity K8sToEntity(JToken resource, EntitySelector entitySelector)
        {
            string domainId = this.EvaluateValueExpression(resource, entitySelector.domainIdQuery, nameof(entitySelector.domain_id_expr), entitySelector.domain_id_expr);
            string semanticId = this.EvaluateValueExpression(resource, entitySelector.semanticIdQuery, nameof(entitySelector.semantic_id_expr), entitySelector.semantic_id_expr);
            string displayName = this.EvaluateValueExpression(resource, entitySelector.displayNameQuery, nameof(entitySelector.display_name_expr), entitySelector.display_name_expr);

            Entity entity = new Entity(
                type: entitySelector.entity_type,
                domain_id: domainId,
                semantic_id: semanticId,
                display_name: displayName
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
