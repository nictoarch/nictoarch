using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jsonata.Net.Native;
using Nictoarch.Modelling.Core.Yaml;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;

namespace Nictoarch.Modelling.Json
{
    public sealed class ProviderConfig
    {
        public enum ESourceTransform
        {
            none,
            xml2json
        }

        [Required] public string source { get; set; } = default!;
        [Required] public ESourceTransform source_transform { get; set; } = ESourceTransform.none;
    }
}
