using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nictoarch.Modelling.K8s
{
    internal sealed class ApiInfo
    {
        public string api_group { get; set; } = default!;
        public string resource_singular { get; set; } = default!;
        public string resource_plural { get; set; } = default!;
        public bool namespaced { get; set; } = default!;
    }
}
