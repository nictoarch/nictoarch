using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jsonata.Net.Native;
using Nictoarch.Modelling.Core.Yaml;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;

namespace Nictoarch.Modelling.Json
{
    public sealed class EntityProviderSpec : IYamlOnDeserialized
    {
        public enum ESourceTransform
        {
            none,
            xml2json
        }

        [Required] public string source { get; set; } = default!;
        [Required] public ESourceTransform source_transform { get; set; } = ESourceTransform.none;
        [Required] public string query { get; set; } = default!;

        internal JsonataQuery entityQuery = default!;

        void IYamlOnDeserialized.OnDeserialized(ParsingEvent parsingEvent)
        {
            try
            {
                entityQuery = new JsonataQuery(query);
            }
            catch (Exception ex)
            {
                throw new YamlException(parsingEvent.Start, parsingEvent.End, $"Failed to parse '{nameof(query)}': {ex.Message}", ex);
            }
        }
    }
}
