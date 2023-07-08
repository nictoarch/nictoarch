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
    public sealed class K8sModelProvider : IEntityProvider<EntityProviderSpec>
    {
        string IProviderBase.Name => "k8s";

        async Task<List<Entity>> IEntityProvider<EntityProviderSpec>.GetEntitiesAsync(EntityProviderSpec spec, CancellationToken cancellationToken)
        {
            KubernetesClientConfiguration config = spec.GetConfiguration();
            List<Entity> results = new List<Entity>();
            using (K8sClient client = new K8sClient(config))
            {
                foreach (EntitySelector selector in spec.selectors)
                {
                    await GetEntities(client, selector, results, cancellationToken);
                    cancellationToken.ThrowIfCancellationRequested();
                }
            }
            return results;
        }


        private static async Task GetEntities(K8sClient client, EntitySelector entitySelector, List<Entity> results, CancellationToken cancellationToken)
        {
            IReadOnlyList<JToken> resources = await GetResources(client, entitySelector, cancellationToken);
            foreach (JToken resource in resources)
            {
                Entity entity = K8sToEntity(resource, entitySelector);
                results.Add(entity);
                cancellationToken.ThrowIfCancellationRequested();
            }
        }

        private static async Task<IReadOnlyList<JToken>> GetResources(K8sClient client, SelectorBase selector, CancellationToken cancellationToken)
        {
            IReadOnlyList<JToken> resources = await client.GetResources(
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

        private static Entity K8sToEntity(JToken resource, EntitySelector entitySelector)
        {
            string domainId = EvaluateValueExpression(resource, entitySelector.domainIdQuery, nameof(entitySelector.domain_id_expr), entitySelector.domain_id_expr);
            string semanticId = EvaluateValueExpression(resource, entitySelector.semanticIdQuery, nameof(entitySelector.semantic_id_expr), entitySelector.semantic_id_expr);
            string displayName = EvaluateValueExpression(resource, entitySelector.displayNameQuery, nameof(entitySelector.display_name_expr), entitySelector.display_name_expr);

            Entity entity = new Entity(
                type: entitySelector.entity_type,
                domain_id: domainId,
                semantic_id: semanticId,
                display_name: displayName
            );

            return entity;
        }

        private static string EvaluateValueExpression(JToken objectTree, JsonataQuery query, string expressionName, string expressionValue)
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
