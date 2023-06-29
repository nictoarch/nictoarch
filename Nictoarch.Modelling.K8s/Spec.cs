using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Jsonata.Net.Native;

namespace Nictoarch.Modelling.K8s
{
    public sealed class Spec
    {
        //entity_type => selectors list
        public List<EntitySelector> entities { get; set; } = default!;
        public List<LinkSelector> links { get; set; } = default!;

        public sealed class EntitySelector: IJsonOnDeserialized
        {
            public enum ResourceKind
            {
                Service
            }

            [JsonRequired] public string entity_type { get; set; } = default!;
            [JsonRequired] public ResourceKind resource_kind { get; set; } = default!;
            [JsonRequired] public string @namespace { get; set; } = default!;
            [JsonRequired] public string label_query { get; set; } = default!; //see https://kubernetes.io/docs/concepts/overview/working-with-objects/labels/#label-selectors

            public string domain_id_expr { get; set; } = "metadata.uid";
            public string semantic_id_expr { get; set; } = "metadata.name + '@' + metadata.namespace";
            public string display_name_expr { get; set; } = "metadata.name + '@' + metadata.namespace";

            internal JsonataQuery domainIdQuery = default!;
            internal JsonataQuery semanticIdQuery = default!;
            internal JsonataQuery displayNameQuery = default!;

            void IJsonOnDeserialized.OnDeserialized()
            {
                if (String.IsNullOrWhiteSpace(this.@namespace))
                {
                    throw new Exception($"Bad value of '{nameof(this.@namespace)}': either specify a namespace name or use '*'");
                }

                if (this.label_query == "")
                {
                    throw new Exception($"Bad value of '{nameof(this.label_query)}': either specify a valid K8s label algebra selector, or use 'null'");
                }

                try
                {
                    this.domainIdQuery = new JsonataQuery(this.domain_id_expr);
                }
                catch (Exception ex)
                {
                    throw new Exception($"Failed to parse '{nameof(this.domain_id_expr)}': {ex.Message}", ex);
                }

                try
                {
                    this.semanticIdQuery = new JsonataQuery(this.semantic_id_expr);
                }
                catch (Exception ex)
                {
                    throw new Exception($"Failed to parse '{nameof(this.semantic_id_expr)}': {ex.Message}", ex);
                }

                try
                {
                    this.displayNameQuery = new JsonataQuery(this.display_name_expr);
                }
                catch (Exception ex)
                {
                    throw new Exception($"Failed to parse '{nameof(this.display_name_expr)}': {ex.Message}", ex);
                }
            }
        }

        public sealed class LinkSelector
        {
            public enum ResourceKind
            {
                ServiceLinkCRD
            }

            [JsonRequired] public ResourceKind resource_kind { get; set; } = default!;
            [JsonRequired] public string @namespace { get; set; } = default!;
            [JsonRequired] public string label_query { get; set; } = default!; //see https://kubernetes.io/docs/concepts/overview/working-with-objects/labels/#label-selectors

            public string from_semantic_id_expr { get; set; } = "spec.from.service + '@' + metadata.namespace";
            public string to_semantic_id_expr { get; set; } = "spec.to.service + '@' + spec.to.namespace";

            public string domain_id_expr { get; set; } = "metadata.uid";
            public string semantic_id_expr { get; set; } = "metadata.name + '@' + metadata.namespace";
            public string display_name_expr { get; set; } = "spec.from.service + '@' + metadata.namespace + ' -> ' +  spec.to.service + ':' + spec.to.port + '@' + spec.to.namespace";

            public void Validate()
            {
                if (String.IsNullOrWhiteSpace(this.@namespace))
                {
                    throw new Exception($"Bad value of '{nameof(this.@namespace)}': either specify a namespace name or use '*'");
                }

                if (this.label_query == "")
                {
                    throw new Exception($"Bad value of '{nameof(this.label_query)}': either specify a valid K8s label algebra selector, or use 'null'");
                }
            }
        }

    }
}
