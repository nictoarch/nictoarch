using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using k8s;
using Nictoarch.Modelling.Core.Yaml;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace Nictoarch.Modelling.K8s.Spec
{
    public abstract class ProviderSpecBase: IYamlOnDeserialized
    {
        /**

        may be specified as 
        
        connect_via: config_file

        or as 

        connect_via:
          config_file: "~/.kube/config"

        */
        public ConnectVia? connect_via { get; set; }

        public double? connect_timeout_seconds { get; set; } = null;

        void IYamlOnDeserialized.OnDeserialized(ParsingEvent parsingEvent)
        {
            if (this.connect_via == null)
            {
                this.connect_via = new ConnectVia() {
                    type = ConnectViaType.auto,
                };
            }
        }

        internal KubernetesClientConfiguration GetConfiguration()
        {
            return K8sClient.GetConfiguration(this.connect_via!.type, this.connect_via!.config_file, this.connect_timeout_seconds);
        }

        public enum ConnectViaType
        {
            auto,
            config_file,
            cluster
        }

        public sealed class ConnectVia : IYamlConvertible
        {
            public ConnectViaType type { get; set; } = ConnectViaType.auto;
            public string? config_file { get; set; }

            void IYamlConvertible.Read(IParser parser, Type expectedType, ObjectDeserializer nestedObjectDeserializer)
            {
                if (parser.TryConsume<Scalar>(out Scalar? value))
                {
                    string strValue = value.Value;
                    if (!Enum.TryParse(strValue, out ConnectViaType parsedType))
                    {
                        throw new YamlException(value.Start, value.End, $"Unexpected {nameof(ProviderSpecBase.connect_via)} value: '{strValue}'. Specify either one of {String.Join(", ", Enum.GetValues<ConnectViaType>())}, or map with details");
                    }
                    this.type = parsedType;
                }
                else
                {
                    ConnectViaImpl impl = (ConnectViaImpl)nestedObjectDeserializer.Invoke(typeof(ConnectViaImpl))!;
                    this.type = ConnectViaType.config_file;
                    this.config_file = impl.config_file;
                }
            }

            void IYamlConvertible.Write(IEmitter emitter, ObjectSerializer nestedObjectSerializer)
            {
                throw new NotImplementedException();
            }

            private sealed class ConnectViaImpl
            {
                [Required] public string config_file { get; set; } = default!;
            }
        }


    }
}
