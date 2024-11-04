using Nictoarch.Modelling.Core.Elements;
using Nictoarch.Modelling.Core.Spec;

namespace Nictoarch.Modelling.Core.Tests
{
    public class SimpleSpecLoadFileInplaceValues
    {
        [Test]
        public async Task SimpleSpecLoadTest()
        {
            SourceRegistry registry = new SourceRegistry();
            string specFile = TestHelpers.GetModelSpecPath(this);
            Console.WriteLine("Loading model from " + specFile);
            
            ModelSpec modelSpec = ModelSpec.LoadFromFile(specFile, registry);
            Assert.That(modelSpec.Name == "Test model");

            Model model = await modelSpec.GetModelAsync();
            Assert.That(model.name == modelSpec.Name);
            Assert.That(model.entities.Count == 2);
            Assert.That(model.links.Count == 0);
        }
    }
}