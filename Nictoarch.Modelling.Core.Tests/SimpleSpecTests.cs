using Nictoarch.Modelling.Core.Elements;
using Nictoarch.Modelling.Core.Spec;

namespace Nictoarch.Modelling.Core.Tests
{
    public class SimpleSpecTests
    {
        private readonly Model m_referenceModel = new Model() {
            name = "Reference Model",
            entities = new List<Entity>() {
                new Entity() {
                    id = "1",
                    type = "sample"
                },
                new Entity() {
                    id = "2",
                    type = "sample"
                }
            }
        };


        [TestCaseSource(nameof(GetSimpleSpecTests))]
        public async Task DoTest(string testDir)
        {
            string envFile = Path.Combine(testDir, "env.txt");
            if (File.Exists(envFile))
            {
                foreach (string line in await File.ReadAllLinesAsync(envFile))
                {
                    if (String.IsNullOrWhiteSpace(line))
                    {
                        continue;
                    }
                    string[] parts = line.Split('=');
                    if (parts.Length != 2)
                    {
                        throw new Exception($"Failed to parse env file {envFile}, line '{line}' does not have a form of <var_name>=<value>");
                    }
                    parts[0] = parts[0].Trim();
                    parts[1] = parts[1].Trim();
                    Console.WriteLine($"Setting env {parts[0]} = '{parts[1]}'");
                    Environment.SetEnvironmentVariable(parts[0], parts[1]);
                }
            }

            SourceRegistry registry = new SourceRegistry();

            string specFile = Path.Combine(testDir, "model.spec.yaml");
            Console.WriteLine("Loading model from " + specFile);

            ModelSpec modelSpec = ModelSpec.LoadFromFile(specFile, registry);
            Assert.That(modelSpec.Name == "Test model");

            Model model = await modelSpec.GetModelAsync();
            Assert.That(model.name == modelSpec.Name);
            Assert.That(model.entities.Count == 2);
            Assert.That(model.links.Count == 0);

            ModelComparison comparison = new ModelComparison(this.m_referenceModel, model);
            Assert.That(comparison.entities_not_in_check_count == 0);
            Assert.That(comparison.entities_not_in_ref_count == 0);
        }

        public static IEnumerable<TestCaseData> GetSimpleSpecTests()
        {
            return Directory.EnumerateDirectories("../../../data/SimpleSpecTests")
                .Select(path => {
                    TestCaseData data = new TestCaseData(path);
                    data.SetName(Path.GetFileName(path));
                    return data;
                });
        }
    }
}