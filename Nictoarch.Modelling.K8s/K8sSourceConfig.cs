using System;
using Nictoarch.Modelling.Core.Spec;

namespace Nictoarch.Modelling.K8s
{
    public sealed class K8sSourceConfig : SourceConfigBase
    {
        public string? config_file { get; set; } = null;
        public bool use_cluster_config { get; set; } = false;
        public double? connect_timeout_seconds { get; set; } = null;
    }
}
