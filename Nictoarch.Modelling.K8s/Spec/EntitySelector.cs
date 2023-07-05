using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Nictoarch.Modelling.K8s.Spec
{
    public sealed class EntitySelector : SelectorBase
    {
        [Required] public string entity_type { get; set; } = default!;
    }
}
