using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jsonata.Net.Native;

namespace Nictoarch.Modelling.Core.Spec
{
    public sealed class Element
    {
        [Required] public ExtractConfigBase extract { get; set; } = default!;
        public JsonataQuery? filter { get; set; }
        public EntitiesSelectorBase? entities { get; set; }
        public JsonataQuery? invalid { get; set; }
    }
}
