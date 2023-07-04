using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nictoarch.Modelling.K8s.Spec
{
    public sealed class EntityProviderSpec: ProviderSpecBase
    {
        public List<EntitySelector> selectors { get; set; } = default!;
    }
}
