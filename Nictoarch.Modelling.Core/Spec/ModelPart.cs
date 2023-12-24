using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nictoarch.Modelling.Core.Spec
{
    public sealed class ModelPart
    {
        [Required] public SourceConfigBase source { get; set; } = default!;
        [Required] public List<Element> elements { get; set; } = default!;
    }
}
