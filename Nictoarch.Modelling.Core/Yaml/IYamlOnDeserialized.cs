using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Core.Events;

namespace Nictoarch.Modelling.Core.Yaml
{
    public interface IYamlOnDeserialized
    {
        void OnDeserialized(ParsingEvent parsingEvent);
    }
}
