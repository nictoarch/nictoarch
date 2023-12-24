using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nictoarch.Modelling.Core.Spec
{
    public sealed class ModelSpecImpl
    {
        [Required] public string name { get; set; } = default!;
        [Required] public List<ModelPart> data { get; set; } = default!;
    }
}
