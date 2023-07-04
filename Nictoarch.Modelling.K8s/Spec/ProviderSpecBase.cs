using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using k8s;

namespace Nictoarch.Modelling.K8s.Spec
{
    public abstract class ProviderSpecBase
    {
        public enum ConnectVia
        {
            auto,
            config_file,
            cluster
        }

        public ConnectVia connect_via { get; set; } = ConnectVia.auto;
        public double? connect_timeout_seconds { get; set; } = null;

        internal KubernetesClientConfiguration GetConfiguration()
        {
            return K8sClient.GetConfiguration(this.connect_via, this.connect_timeout_seconds);
        }
    }
}
