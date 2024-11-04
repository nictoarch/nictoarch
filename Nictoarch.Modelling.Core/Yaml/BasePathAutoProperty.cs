using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nictoarch.Modelling.Core.Yaml
{
    // fields of this type are automatically populated by the ModelSpecObjectFactory (if specified in yaml)
    // fields of this type are automatically injected by AutoPropertyDeserializer (if not specified in yaml)
    public sealed class BasePathAutoProperty
    {
        public string path { get; set; }

        public BasePathAutoProperty(string path)
        {
            this.path = path;
        }
    }
}
