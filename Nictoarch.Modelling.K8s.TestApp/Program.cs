using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Nictoarch.Common;
using Nictoarch.Modelling.Core.Elements;

namespace Nictoarch.Modelling.K8s.TestApp
{
    internal class Program
    {
        static Task Main(string[] args)
        {
            return ProgramHelper.MainWrapperAsync(MainInternal);
        }

        static async Task MainInternal()
        {
            ModelSpec spec = new ModelSpec() {
                model_name = "test",
                /*
                entities = new List<ModelSpec.EntitySelector>() {
                    new ModelSpec.EntitySelector() {
                        entity_type = "deploy",
                        api_group = "apps",
                        resource_kind = "deployment",
                    }
                }
                ,*/
                entities = new List<ModelSpec.EntitySelector>() {
                    new ModelSpec.EntitySelector() {
                        entity_type = "service",
                        api_group = "v1",
                        resource_kind = "service",
                    }
                }
            };

            foreach (IJsonOnDeserialized entity in spec.entities)
            {
                entity.OnDeserialized();
            }

            K8sModelProviderStub provider = new K8sModelProviderStub();

            Model model = await provider.GetModelAsync(spec);

            Console.WriteLine(model.ToString());
        }
    }
}