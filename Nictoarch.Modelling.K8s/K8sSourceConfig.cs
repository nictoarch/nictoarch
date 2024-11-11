using System;
using System.ComponentModel.DataAnnotations;
using Nictoarch.Modelling.Core.Spec;

namespace Nictoarch.Modelling.K8s
{
    public sealed class K8sSourceConfig : SourceConfigBase
    {
        [Required] public Config config { get; set; } = default!;
        public double? connect_timeout_seconds { get; set; } = null;

        public abstract class Config
        {
            [Required] public string type { get; set; } = default!;

            //used for short form "auth: none"
            public static Config Parse(string v)
            {
                if (v == ClusterConfig.TYPE)
                {
                    return new ClusterConfig() {
                        type = ClusterConfig.TYPE,
                    };
                }
                else if (v == FileConfig.TYPE)
                {
                    return new FileConfig() {
                        type = FileConfig.TYPE,
                    };
                }
                else
                {
                    throw new ArgumentException($"Unexpected value wile parsing {nameof(K8sSourceConfig.config)} field: '{v}'");
                }
            }

        }

        public sealed class ClusterConfig: Config
        {
            public const string TYPE = "cluster";
        }

        public sealed class FileConfig: Config
        {
            public const string TYPE = "file";
            public string? file { get; set; } = null;
        }
    }
}
