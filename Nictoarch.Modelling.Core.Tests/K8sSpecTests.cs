using Nictoarch.Modelling.Core.Elements;
using Nictoarch.Modelling.Core.Spec;

namespace Nictoarch.Modelling.Core.Tests
{
    public class K8sSpecTests
    {
        [TestCaseSource(nameof(GetK8sSpecTests))]
        public void DoTest(string testDir)
        {
            SourceRegistry registry = new SourceRegistry();

            string specFile = Path.Combine(testDir, "model.spec.yaml");
            Console.WriteLine("Loading model from " + specFile);

            ModelSpec modelSpec = ModelSpec.LoadFromFile(specFile, registry);
            Assert.That(modelSpec.Name == "Test model");

            // No way to load model in tests, as there's no test k8s cluster (
            // but at least we check the spec correctness
            /* Model model = await modelSpec.GetModelAsync(); */        
        }

        public static IEnumerable<TestCaseData> GetK8sSpecTests()
        {
            return Directory.EnumerateDirectories("../../../data/K8sSpecTests")
                .Select(path => {
                    TestCaseData data = new TestCaseData(path);
                    data.SetName(Path.GetFileName(path));
                    return data;
                });
        }
    }
}