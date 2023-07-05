using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nictoarch.Modelling.K8s.Spec
{
    public sealed class EntityProviderSpec: ProviderSpecBase
    {
        [Required] public List<EntitySelector> selectors { get; set; } = default!;
    }
}
