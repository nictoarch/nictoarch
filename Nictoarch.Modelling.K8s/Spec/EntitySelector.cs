using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using YamlDotNet.Core.Events;

namespace Nictoarch.Modelling.K8s.Spec
{
    public sealed class EntitySelector : ObjectSelector
    {
        public string? entity_type { get; set; }

        public override void OnDeserialized(ParsingEvent parsingEvent)
        {
            base.OnDeserialized(parsingEvent);

            if (this.entity_type == null)
            {
                this.entity_type = this.resource_kind;
            }
        }
    }
}
