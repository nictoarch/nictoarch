using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Nictoarch.Modelling.Core.Spec;
using Nictoarch.Modelling.Core.Yaml;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;

namespace Nictoarch.Modelling.Db
{
    public sealed class DbSourceConfig : SourceConfigBase, IYamlOnDeserialized
    {
        [Required(AllowEmptyStrings = false)] public string connection { get; set; } = default!;
        public string? connection_string { get; set; } = null;
        public Dictionary<string, object>? connection_args { get; set; } = null;

        public void OnDeserialized(ParsingEvent parsingEvent)
        {
            if (this.connection_string == null)
            {
                if (this.connection_args == null)
                {
                    throw new YamlException(parsingEvent.Start, parsingEvent.End, $"Either '{nameof(this.connection_string)}' or '{nameof(this.connection_args)}' should be specified for database source");
                }
                else
                {
                    IEnumerable<string> args = this.connection_args
                        .Where(p => !String.IsNullOrWhiteSpace(p.Value?.ToString()))
                        .Select(p => $"{p.Key}={p.Value}");
                    this.connection_string = String.Join(";", args);
                }
            }
            else if (this.connection_args != null)
            {
                throw new YamlException(parsingEvent.Start, parsingEvent.End, $"Only one of '{nameof(this.connection_string)}' and '{nameof(this.connection_args)}' may be specified for database source");
            }
        }
    }
}
