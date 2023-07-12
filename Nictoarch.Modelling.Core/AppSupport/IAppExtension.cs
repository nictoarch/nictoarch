using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nictoarch.Modelling.Core.AppSupport
{
    public interface IAppExtension
    {
        string Name { get; }
        string Description { get; }
        List<Command> GetCommands();
    }
}
