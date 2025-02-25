using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jsonata.Net.Native;
using Nictoarch.Modelling.Core.Spec;
using Nictoarch.Modelling.Core.Yaml;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;

namespace Nictoarch.Modelling.K8s
{
    internal sealed class K8sExtractConfig: ExtractConfigBase, IYamlOnDeserialized
    {
        public enum EDataType
        {
            version,    //get cluster version
            resource,   //get k8s resource
        }

        [Required] public EDataType type { get; set; } = EDataType.resource;

        //only allowed if type == resource
        public string? api_group { get; set; } = null; //eg "apps" or "apps/v1". use "v1" or "core" for core resources
        public string? resource_kind { get; set; }      //eg "deployment" or "deployments" 
        public string? @namespace { get; set; } = null; //null or namespace name
        public string? label_query { get; set; } = null; //null or valid label selector, see https://kubernetes.io/docs/concepts/overview/working-with-objects/labels/#label-selectors

        public void OnDeserialized(ParsingEvent parsingEvent)
        {
            switch (this.type)
            {
            case EDataType.version:
                {
                    if (this.resource_kind != null)
                    {
                        throw new YamlException(parsingEvent.Start, parsingEvent.End, $"No value can be specified for '{nameof(this.resource_kind)}' if `{nameof(this.type)} = {nameof(EDataType.version)}`");
                    }
                    if (this.api_group != null)
                    {
                        throw new YamlException(parsingEvent.Start, parsingEvent.End, $"No value can be specified for '{nameof(this.api_group)}' if `{nameof(this.type)} = {nameof(EDataType.version)}`");
                    }
                    if (this.@namespace != null)
                    {
                        throw new YamlException(parsingEvent.Start, parsingEvent.End, $"No value can be specified for '{nameof(this.@namespace)}' if `{nameof(this.type)} = {nameof(EDataType.version)}`");
                    }
                    if (this.label_query != null)
                    {
                        throw new YamlException(parsingEvent.Start, parsingEvent.End, $"No value can be specified for '{nameof(this.label_query)}' if `{nameof(this.type)} = {nameof(EDataType.version)}`");
                    }
                }
                break;
            case EDataType.resource:
                {
                    if (String.IsNullOrEmpty(this.resource_kind))
                    {
                        throw new YamlException(parsingEvent.Start, parsingEvent.End, $"No value specified for '{nameof(this.resource_kind)}'.");
                    }

                    if (this.@namespace == "")
                    {
                        throw new YamlException(parsingEvent.Start, parsingEvent.End, $"Bad value of '{nameof(this.@namespace)}': either specify a namespace name or use 'null'");
                    }

                    if (this.label_query == "")
                    {
                        throw new YamlException(parsingEvent.Start, parsingEvent.End, $"Bad value of '{nameof(this.label_query)}': either specify a valid K8s label algebra selector, or use 'null'");
                    }
                }
                break;
            default:
                throw new YamlException(parsingEvent.Start, parsingEvent.End, $"Unexpected value for '{nameof(this.type)}' field: '{this.type}'. Possible vaules are: {String.Join(", ", Enum.GetNames<EDataType>())}. Default is '{nameof(EDataType.resource)}'");
            }
        }
    }
}
