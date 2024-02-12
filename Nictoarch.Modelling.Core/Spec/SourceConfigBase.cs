using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nictoarch.Modelling.Core.Spec
{
    public abstract class SourceConfigBase
    {
        [Required] public string type { get; set; } = default!;
    }
}
