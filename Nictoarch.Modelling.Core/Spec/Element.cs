using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jsonata.Net.Native;
using Nictoarch.Modelling.Core.Yaml;

namespace Nictoarch.Modelling.Core.Spec
{
    public sealed class Element
    {
        [Required] public ExtractConfigBase extract { get; set; } = default!;
        public JsonataQuery? filter { get; set; }
        public EntitiesSelectorBase? entities { get; set; }
        public LinksSelectorBase? links { get; set; }
        public JsonataQueryYamlWrapper? invalid { get; set; } //using wrapper instead of a query as a workaround for using !inpace and other tags because those only return string, https://github.com/aaubry/YamlDotNet/issues/368
    }
}
