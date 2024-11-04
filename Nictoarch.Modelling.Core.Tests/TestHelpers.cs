using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nictoarch.Modelling.Core.Tests
{
    internal static class TestHelpers
    {
        internal static string GetModelSpecPath(object testObject)
        {
            return Path.GetFullPath(Path.Combine(@$"../../../data/{testObject.GetType().Name}/model.spec.yaml"));
        }
    }
}
