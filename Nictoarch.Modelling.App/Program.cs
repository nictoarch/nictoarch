using System;
using System.CommandLine;
using System.IO;
using System.Threading.Tasks;
using Nictoarch.Common;
using Nictoarch.Modelling.Core;
using Nictoarch.Modelling.Core.Elements;
using NLog;

namespace Nictoarch.Modelling.App
{
    internal class Program
    {
        private static readonly Logger s_logger = LogManager.GetCurrentClassLogger();

        static Task Main(string[] args)
        {
            return ProgramHelper.MainWrapperAsync(() => MainInternal(args));
        }

        private static async Task<int> MainInternal(string[] args)
        {
            RootCommand rootCommand = new RootCommand("Sample app for System.CommandLine");


            Argument<string> specArg = new Argument<string>("spec", "Path to spec yaml file");
            Option<string?> outputOpt = new Option<string?>(new string[] { "--out", "-o" }, () => null, "Output file name");
            Command exportModelCommand = new Command("export-model", "Export model accoring to spec-file") {
                specArg,
                outputOpt
            };
            exportModelCommand.AddAlias("e");
            exportModelCommand.SetHandler(
                ExportModel, 
                specArg, outputOpt
            );

            rootCommand.AddCommand(exportModelCommand);

            return await rootCommand.InvokeAsync(args);
        }

        private static async Task ExportModel(string specFile, string? outputFile)
        {
            if (outputFile == null)
            {
                outputFile = specFile + ".json";
            }

            ModelProviderRegistry registry = new ModelProviderRegistry();

            s_logger.Info("Reading model spec file " + specFile);
            ModelSpec modelSpec = ModelSpec.LoadFromFile(specFile, registry);

            s_logger.Info("Retrieving model " + modelSpec.Name);
            Model model = await modelSpec.GetModelAsync();
            s_logger.Info($"Got {model.entities?.Count ?? 0} entities, and {model.links?.Count ?? 0} links");

            s_logger.Info("Writing model to " + outputFile);
            await File.WriteAllTextAsync(outputFile, model.ToJson().ToIndentedString());
        }
    }
}