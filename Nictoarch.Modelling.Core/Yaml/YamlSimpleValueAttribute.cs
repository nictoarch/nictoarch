using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nictoarch.Modelling.Core.Yaml
{
    // used together with IMaybeYamlSimpleValue to allow deserializing classes from a scalar (string)
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class YamlSimpleValueAttribute : Attribute
    {
    }
}
