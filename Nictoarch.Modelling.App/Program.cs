using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Nictoarch.Common;
using Nictoarch.Modelling.Core;
using Nictoarch.Modelling.Core.Elements;
using Nictoarch.Modelling.Core.Yaml;
using YamlDotNet.Serialization;

namespace Nictoarch.Modelling.App
{
    internal class Program
    {
        static Task Main(string[] args)
        {
            return ProgramHelper.MainWrapperAsync(MainInternal);
        }

        private static async Task MainInternal()
        {
            ModelProviderRegistry registry = new ModelProviderRegistry();
            ModelSpec modelSpec = ModelSpec.LoadFromFile("../../../../Nictoarch.Modelling.Core/model_spec_sample.yaml", registry);
            Model model = await modelSpec.GetModelAsync();

            Console.WriteLine(model.ToJson().ToIndentedString());
        }
    }
}