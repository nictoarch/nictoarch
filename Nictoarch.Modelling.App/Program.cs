using System;
using System.CommandLine;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Nictoarch.Common;
using Nictoarch.Modelling.Core;
using Nictoarch.Modelling.Core.AppSupport;
using Nictoarch.Modelling.Core.Elements;
using NLog;

namespace Nictoarch.Modelling.App
{
    internal class Program
    {
        private static readonly Logger s_logger = LogManager.GetCurrentClassLogger();
        private static readonly ModelProviderRegistry s_registry = new ModelProviderRegistry();

        static Task Main(string[] args)
        {
            return ProgramHelper.MainWrapperAsync(() => MainInternal(args));
        }

        private static async Task<int> MainInternal(string[] args)
        {
            RootCommand rootCommand = new RootCommand($"{nameof(Nictoarch)} modelling App");

            {
                Command exportModelCommand = new Command("extract-model", "Extract model accoring to spec-file");
                exportModelCommand.AddAlias("e");
                exportModelCommand.AddAlias("extract");
                Argument<string> specArg = new Argument<string>("spec", "Path to spec yaml file");
                Option<string?> outputOpt = new Option<string?>(new string[] { "--out", "-o" }, () => null, "Output file name");
                exportModelCommand.Add(specArg);
                exportModelCommand.Add(outputOpt);
                exportModelCommand.SetHandler(
                    ExtractModelCommand,
                    specArg, outputOpt
                );
                rootCommand.AddCommand(exportModelCommand);
            }

            {
                Command compareModelsCommand = new Command("compare-models", "Compare two models");
                compareModelsCommand.AddAlias("c");
                compareModelsCommand.AddAlias("compare");
                Option<string?> refSpecOpt = new Option<string?>("--ref-spec", () => null, "Spec file for REF model");
                Option<string?> refModelOpt = new Option<string?>("--ref-model", () => null, "Model file for REF model");
                Option<bool> refFlagOpt = new Option<bool>("--extract-ref-model-even-if-exitst", "Extract REF model from Spec file even if Model file exists (will also overwrite the REF Model file)");
                Option<string?> checkSpecOpt = new Option<string?>("--check-spec", () => null, "Spec file for CHECK model");
                Option<string?> checkModelOpt = new Option<string?>("--check-model", () => null, "Model file for CHECK model");
                Option<bool> checkFlagOpt = new Option<bool>("--extract-check-model-even-if-exitst", "Extract CHECK model from Spec file even if Model file exists (will also overwrite the CHECK Model file)");
                Option<string?> outputFileOpt = new Option<string?>("--output-file", () => null, "File name to write diff to");
                Option<bool> throwOnMismatch = new Option<bool>("--throw-on-mismatch", "Whenever to throw excheption when models differ");
                compareModelsCommand.Add(refSpecOpt);
                compareModelsCommand.Add(refModelOpt);
                compareModelsCommand.Add(refFlagOpt);
                compareModelsCommand.Add(checkSpecOpt);
                compareModelsCommand.Add(checkModelOpt);
                compareModelsCommand.Add(checkFlagOpt);
                compareModelsCommand.Add(outputFileOpt);
                compareModelsCommand.Add(throwOnMismatch);
                compareModelsCommand.SetHandler(
                    CompareModelsCommand,
                    refSpecOpt, refModelOpt, refFlagOpt,
                    checkSpecOpt, checkModelOpt, checkFlagOpt,
                    outputFileOpt, throwOnMismatch
                );
                rootCommand.AddCommand(compareModelsCommand);
            }

            AppExtensionsRegistry extensionRegistry = new AppExtensionsRegistry();
            extensionRegistry.PopulateRootCommand(rootCommand);

            return await rootCommand.InvokeAsync(args);
        }

        private static async Task ExtractModelCommand(string specFile, string? outputFile)
        {
            if (outputFile == null)
            {
                outputFile = specFile + ".json";
            }

            Model model = await ExtractModel(specFile);

            await SaveModelToFile(model, outputFile);
        }

        private static async Task CompareModelsCommand(
            string? refSpecFile, string? refModelFile, bool extractRefModelEvenIfExists,
            string? checkSpecFile, string? checkModelFile, bool extractCheckModelEvenIfExists,
            string? outputDiffFileName, bool throwOnMismatch
        )
        {
            Model refModel, checkModel;
            try
            {
                refModel = await ExtractOrLoadModel(refSpecFile, refModelFile, extractRefModelEvenIfExists);
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to get REF model: " + ex.Message, ex);
            }

            try
            {
                checkModel = await ExtractOrLoadModel(checkSpecFile, checkModelFile, extractCheckModelEvenIfExists);
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to get CHECK model: " + ex.Message, ex);
            }

            s_logger.Trace("Building diff");
            ModelComparison diff = ModelComparison.Build(refModel, checkModel);

            string? diffMessage;
            if (diff.ModelsAreSame())
            {
                s_logger.Trace($"Models are same!");
                diffMessage = null;
            }
            else
            {
                diffMessage = $"Models differ! Got {diff.entities_not_in_check.Count} entitites not in CHECK model, {diff.entities_not_in_ref.Count} entites not in REF model";
                s_logger.Trace(diffMessage);
            }

            if (outputDiffFileName != null)
            {
                await SaveDiffToFile(diff, outputDiffFileName);
            }

            if (throwOnMismatch && diffMessage != null)
            {
                throw new Exception(diffMessage);
            }
        }

        private static async Task<Model> ExtractOrLoadModel(string? specFile, string? modelFile, bool extractModelEvenWhenExists)
        {
            if (specFile != null)
            {
                if (modelFile != null 
                    && !extractModelEvenWhenExists
                    && File.Exists(modelFile)
                )
                {
                    return await LoadModelFromFile(modelFile);
                }

                Model model = await ExtractModel(specFile);

                if (modelFile != null)
                {
                    await SaveModelToFile(model, modelFile);
                }
                return model;
            }
            else if (modelFile != null)
            {
                return await LoadModelFromFile(modelFile);
            }
            else
            {
                throw new Exception("Either model file or spec file should be specified");
            }
        }

        private static Task SaveModelToFile(Model model, string outputFile)
        {
            s_logger.Info("Writing model to " + outputFile);
            return File.WriteAllTextAsync(outputFile, model.ToJson().ToIndentedString());
        }

        private static async Task<Model> ExtractModel(string specFile)
        {
            s_logger.Info("Reading model spec file " + specFile);
            ModelSpec modelSpec = ModelSpec.LoadFromFile(specFile, s_registry);

            s_logger.Info("Retrieving model " + modelSpec.Name);
            Model model = await modelSpec.GetModelAsync();
            s_logger.Info($"Got {model.entities?.Count ?? 0} entities, and {model.links?.Count ?? 0} links");

            return model;
        }

        private static async Task<Model> LoadModelFromFile(string modelFile)
        {
            s_logger.Trace("Loading model from " + modelFile);
            using (Stream stream = File.OpenRead(modelFile))
            {
                return await Model.FromJson(stream);
            }
        }

        private static async Task SaveDiffToFile(ModelComparison diff, string file)
        {
            s_logger.Trace("Saving diff to " + file);
            using (Stream stream = File.Create(file))
            {
                await JsonSerializer.SerializeAsync(stream, diff, new JsonSerializerOptions() { WriteIndented = true });
            }
        }
    }
}