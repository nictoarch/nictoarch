using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Nictoarch.Modelling.K8s.Spec
{
    public sealed class EntitySelector : SelectorBase
    {
        [JsonRequired] public string entity_type { get; set; } = default!;
    }
}
