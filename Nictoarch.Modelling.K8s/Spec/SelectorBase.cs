﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Jsonata.Net.Native;

namespace Nictoarch.Modelling.K8s.Spec
{
    public abstract class SelectorBase : IJsonOnDeserialized
    {
        [JsonRequired] public string api_group { get; set; } = default!; //eg "apps" or "apps/v1". use "v1" or "core" for core resources
        [JsonRequired] public string resource_kind { get; set; } = default!; //eg "deployment" or "deployments"
        public string? @namespace { get; set; } //null or namespace name
        public string? label_query { get; set; } //null or valid label selector, see https://kubernetes.io/docs/concepts/overview/working-with-objects/labels/#label-selectors

        public string domain_id_expr { get; set; } = "metadata.uid";
        public string semantic_id_expr { get; set; } = "metadata.name & '@' & metadata.namespace";
        public string display_name_expr { get; set; } = "metadata.name & '@' & metadata.namespace";

        internal JsonataQuery domainIdQuery = default!;
        internal JsonataQuery semanticIdQuery = default!;
        internal JsonataQuery displayNameQuery = default!;

        void IJsonOnDeserialized.OnDeserialized()
        {
            if (this.@namespace == "")
            {
                throw new Exception($"Bad value of '{nameof(this.@namespace)}': either specify a namespace name or use 'null'");
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
}
