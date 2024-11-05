using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nictoarch.Modelling.Core.Yaml
{
    // extend this interface to allow deserializing object from a single scalar (string)
    // mark one property of your type with YamlSimpleValueAttribute
    public interface IMaybeYamlSimpleValue
    {
    }
}
