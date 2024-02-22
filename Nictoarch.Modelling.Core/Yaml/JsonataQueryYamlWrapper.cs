using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jsonata.Net.Native;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace Nictoarch.Modelling.Core.Yaml
{
    public sealed class JsonataQueryYamlWrapper
    {
        public JsonataQuery query { get; set; } = default!;

        public static JsonataQueryYamlWrapper Parse(string value)
        {
            return new JsonataQueryYamlWrapper() {
                query = new JsonataQuery(value),
            };
        }
    }
}
