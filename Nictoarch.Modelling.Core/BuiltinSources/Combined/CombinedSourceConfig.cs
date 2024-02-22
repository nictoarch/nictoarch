using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Net.Http.Headers;
using System.Text;
using Nictoarch.Modelling.Core.Spec;

namespace Nictoarch.Modelling.Core.BuiltinSources.Combined
{
    public sealed class CombinedSourceConfig: SourceConfigBase
    {
        [Required] public Dictionary<string, SubElementConfig> sources { get; set; } = default!;

        public sealed class SubElementConfig
        {
            [Required] public SourceConfigBase source { get; set; } = default!;
            [Required] public ExtractConfigBase extract { get; set; } = default!;
        }
    }
}
