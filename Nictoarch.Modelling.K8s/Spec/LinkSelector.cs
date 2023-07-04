using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Nictoarch.Modelling.K8s.Spec
{
    public sealed class LinkSelector : SelectorBase
    {
        [JsonRequired] public string from_entity_semantic_id_expr { get; set; } = "spec.from.service & '@' & metadata.namespace";
        [JsonRequired] public string to_entity_semantic_id_expr { get; set; } = "spec.to.service & '@' & spec.to.namespace";
    }
}
