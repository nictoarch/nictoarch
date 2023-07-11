using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Jsonata.Net.Native;
using Nictoarch.Modelling.Core.Yaml;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;

namespace Nictoarch.Modelling.K8s.Spec
{
    public class ValidationSelector: SelectorBase
    {
        public string? transform_expr { get; set; } = null;

        internal JsonataQuery? transformQuery = null;

        public override void OnDeserialized(ParsingEvent parsingEvent)
        {
            base.OnDeserialized(parsingEvent);

            try
            {
                if (this.transform_expr != null)
                {
                    this.transformQuery = new JsonataQuery(this.transform_expr);
                }
            }
            catch (Exception ex)
            {
                throw new YamlException(parsingEvent.Start, parsingEvent.End, $"Failed to parse '{nameof(this.transform_expr)}': {ex.Message}", ex);
            }
        }
    }
}
