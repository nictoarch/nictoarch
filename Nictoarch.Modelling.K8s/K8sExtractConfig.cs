using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jsonata.Net.Native;
using Nictoarch.Modelling.Core;
using Nictoarch.Modelling.Core.Yaml;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;

namespace Nictoarch.Modelling.K8s
{
    internal sealed class K8sExtractConfig: ModelSpec.ExtractConfigBase, IYamlOnDeserialized
    {
        public string? api_group { get; set; } = null; //eg "apps" or "apps/v1". use "v1" or "core" for core resources
        [Required] public string resource_kind { get; set; } = default!; //eg "deployment" or "deployments"
        public string? @namespace { get; set; } //null or namespace name
        public string? label_query { get; set; } //null or valid label selector, see https://kubernetes.io/docs/concepts/overview/working-with-objects/labels/#label-selectors

        public void OnDeserialized(ParsingEvent parsingEvent)
        {
            if (this.@namespace == "")
            {
                throw new YamlException(parsingEvent.Start, parsingEvent.End, $"Bad value of '{nameof(this.@namespace)}': either specify a namespace name or use 'null'");
            }

            if (this.label_query == "")
            {
                throw new YamlException(parsingEvent.Start, parsingEvent.End, $"Bad value of '{nameof(this.label_query)}': either specify a valid K8s label algebra selector, or use 'null'");
            }
        }
    }
}
