using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nictoarch.Modelling.Core;

namespace Nictoarch.Modelling.Json
{
    internal class JsonExtractConfig: ModelSpec.ExtractConfigBase
    {
        public enum ESourceTransform
        {
            none,
            xml2json
        }

        [Required] public ESourceTransform transform { get; set; } = ESourceTransform.none;
    }
}
