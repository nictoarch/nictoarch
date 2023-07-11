using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Jsonata.Net.Native;
using Nictoarch.Modelling.Core.Yaml;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;

namespace Nictoarch.Modelling.K8s.Spec
{
    public abstract class ObjectSelector : SelectorBase, IYamlOnDeserialized
    {
        [Required] public string domain_id_expr { get; set; } = "metadata.uid";
        [Required] public string semantic_id_expr { get; set; } = "metadata.name & '@' & metadata.namespace";
        [Required] public string display_name_expr { get; set; } = "metadata.name & '@' & metadata.namespace";

        internal JsonataQuery domainIdQuery = default!;
        internal JsonataQuery semanticIdQuery = default!;
        internal JsonataQuery displayNameQuery = default!;
        

        public override void OnDeserialized(ParsingEvent parsingEvent)
        {
            base.OnDeserialized(parsingEvent);

            try
            {
                this.domainIdQuery = new JsonataQuery(this.domain_id_expr);
            }
            catch (Exception ex)
            {
                throw new YamlException(parsingEvent.Start, parsingEvent.End, $"Failed to parse '{nameof(this.domain_id_expr)}': {ex.Message}", ex);
            }

            try
            {
                this.semanticIdQuery = new JsonataQuery(this.semantic_id_expr);
            }
            catch (Exception ex)
            {
                throw new YamlException(parsingEvent.Start, parsingEvent.End, $"Failed to parse '{nameof(this.semantic_id_expr)}': {ex.Message}", ex);
            }

            try
            {
                this.displayNameQuery = new JsonataQuery(this.display_name_expr);
            }
            catch (Exception ex)
            {
                throw new YamlException(parsingEvent.Start, parsingEvent.End, $"Failed to parse '{nameof(this.display_name_expr)}': {ex.Message}", ex);
            }
        }
    }
}
