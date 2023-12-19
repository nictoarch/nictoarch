using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Nictoarch.Modelling.Core;

namespace Nictoarch.Modelling.K8s
{
    public sealed class K8sSourceConfig : ModelSpec.SourceConfigBase
    {
        public string? config_file { get; set; } = null;
        public double? connect_timeout_seconds { get; set; } = null;
    }
}
