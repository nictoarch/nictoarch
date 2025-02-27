﻿using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Jsonata.Net.Native.Json;
using Microsoft.IdentityModel.Tokens;
using Nictoarch.Common;
using Nictoarch.Modelling.Core;
using Nictoarch.Modelling.Core.AppSupport;
using Nictoarch.Modelling.Core.Elements;
using Nictoarch.Modelling.Core.Spec;
using NLog;

namespace Nictoarch.Modelling.App
{
    internal class Program
    {
        private static readonly Logger s_logger = LogManager.GetCurrentClassLogger();
        private static readonly SourceRegistry s_registry = new SourceRegistry();

        static Task Main(string[] args)
        {
            return ProgramHelper.MainWrapperAsync(() => MainInternal(args));
        }

        private static async Task<int> MainInternal(string[] args)
        {
            RootCommand rootCommand = new RootCommand($"{nameof(Nictoarch)} modelling App v{Assembly.GetExecutingAssembly().GetName().Version}");

            {
                Command exportModelCommand = new Command("extract-model", "Extract model accoring to spec-file");
                exportModelCommand.AddAlias("e");
                exportModelCommand.AddAlias("extract");
                Argument<string> specArg = new Argument<string>("spec", "Path to spec yaml file");
                Option<bool> validateOpt = new Option<bool>(new string[] { "--validate" }, () => false, "Validate model (with links) before ouptut");
                Option<string?> outputOpt = new Option<string?>(new string[] { "--out", "-o" }, () => null, "Output file name or url");
                exportModelCommand.Add(specArg);
                exportModelCommand.Add(validateOpt);
                exportModelCommand.Add(outputOpt);
                exportModelCommand.SetHandler(
                    ExtractModelCommand,
                    specArg, validateOpt, outputOpt
                );
                rootCommand.AddCommand(exportModelCommand);
            }

            {
                Command mergeModelCommand = new Command("merge-models", "Merge multiple models into one");
                mergeModelCommand.AddAlias("m");
                mergeModelCommand.AddAlias("merge");
                Argument<string> nameArg = new Argument<string>("name", "Name of the new model");
                Option<List<string>> sourcesOpt = new Option<List<string>>(new string[] { "--in", "-i" }, () => new List<string>(), "input file names or patterns");
                Option<bool> validateOpt = new Option<bool>(new string[] { "--validate" }, () => false, "Validate model (with links) before ouptut");
                Option<string?> outputOpt = new Option<string?>(new string[] { "--out", "-o" }, () => null, "Output file name or url");
                mergeModelCommand.Add(nameArg);
                mergeModelCommand.Add(sourcesOpt);
                mergeModelCommand.Add(validateOpt);
                mergeModelCommand.Add(outputOpt);
                mergeModelCommand.SetHandler(
                    MergeModelCommand,
                    nameArg, sourcesOpt, validateOpt, outputOpt
                );
                rootCommand.AddCommand(mergeModelCommand);
            }

            {
                Command validateModelCommand = new Command("validate-model", "Fully validate a model");
                validateModelCommand.AddAlias("v");
                validateModelCommand.AddAlias("validate");
                Argument<string> modelFileArg = new Argument<string>("model", "Model file");
                validateModelCommand.Add(modelFileArg);
                validateModelCommand.SetHandler(
                    ValidateModelCommand,
                    modelFileArg
                );
                rootCommand.AddCommand(validateModelCommand);
            }

            {
                Command compareModelsCommand = new Command("compare-models", "Compare two models");
                compareModelsCommand.AddAlias("c");
                compareModelsCommand.AddAlias("compare");
                Argument<string> refModelArg = new Argument<string>("ref-model", "Model file for REF(erence) model");
                Argument<string> checkModelArg = new Argument<string>("check-model", "Model file for CHECK model");
                Option<string?> outputFileOpt = new Option<string?>(new string[] { "--out", "-o" }, () => null, "File name to write diff to");
                Option<bool> throwOnMismatch = new Option<bool>("--throw-on-mismatch", "Whenever to throw excheption when models differ");
                compareModelsCommand.Add(refModelArg);
                compareModelsCommand.Add(checkModelArg);
                compareModelsCommand.Add(outputFileOpt);
                compareModelsCommand.Add(throwOnMismatch);
                compareModelsCommand.SetHandler(
                    CompareModelsCommand,
                    refModelArg, checkModelArg,
                    outputFileOpt, throwOnMismatch
                );
                rootCommand.AddCommand(compareModelsCommand);
            }

            AppExtensionsRegistry extensionRegistry = new AppExtensionsRegistry();
            extensionRegistry.PopulateRootCommand(rootCommand);

            return await rootCommand.InvokeAsync(args);
        }

        private static async Task ExtractModelCommand(string specFile, bool validate, string? outputFile)
        {
            if (outputFile == null)
            {
                outputFile = specFile + ".json";
            }

            Model model = await ExtractModel(specFile);

            if (validate)
            {
                s_logger.Info("Validating model");
                model.Validate(checkLinkIntegrity: true);
            }

            await OutputModel(model, outputFile);
        }

        private static async Task MergeModelCommand(string modelName, List<string> inputs, bool validate, string? outputFile)
        {
            s_logger.Info($"Creating model '{modelName}'");
            Model model = new Model(modelName, new List<Entity>(), new List<Link>(), new List<object>());

            foreach (string input in inputs)
            {
                if (input.Contains('*') || input.Contains('?'))
                {
                    string directory = Path.GetDirectoryName(input)!;
                    string pattern = Path.GetFileName(input);


                    if (String.IsNullOrWhiteSpace(directory))
                    {
                        directory = ".";
                    }

                    s_logger.Info($"Enumerating models in '{directory}' for pattern '{pattern}'");
                    foreach (string file in Directory.EnumerateFiles(directory, pattern))
                    {
                        AppendModelFromFile(file);
                    }
                }
                else
                {
                    AppendModelFromFile(input);
                }
            }

            s_logger.Info("Merging model done");

            if (validate)
            {
                s_logger.Info("Validating model");
                model.Validate(checkLinkIntegrity: true);
            }

            if (outputFile != null)
            {
                await OutputModel(model, outputFile);
            }

            void AppendModelFromFile(string file)
            {
                Model addModel;
                try
                {
                    addModel = LoadModelFromFile(file);
                }
                catch (Exception ex)
                {
                    throw new Exception("Failed to get model: " + ex.Message, ex);
                }

                model.Merge(addModel);
            }
        }

        private static async Task OutputModel(Model model, string outputFile)
        {
            Uri pushUrl;
            try
            {
                pushUrl = new Uri(outputFile);
            }
            catch (Exception)
            {
                //not an URI
                await SaveModelToFile(model, outputFile);
                return;
            }

            switch (pushUrl.Scheme)
            {
            case "http":
            case "https":
                await PushModelToHttp(model, pushUrl);
                break;
            default:
                throw new Exception($"Unexpected URI shema '{pushUrl.Scheme}'");
            }
        }


        private static Task ValidateModelCommand(string modelFile)
        {
            Model model;
            try
            {
                model = LoadModelFromFile(modelFile);
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to load model: " + ex.Message, ex);
            }

            model.Validate(checkLinkIntegrity: true);

            return Task.CompletedTask;
        }

        private static async Task CompareModelsCommand(string refModelFile, string checkModelFile, string? outputDiffFileName, bool throwOnMismatch)
        {
            Model refModel, checkModel;
            try
            {
                refModel = LoadModelFromFile(refModelFile);
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to get REF model: " + ex.Message, ex);
            }

            try
            {
                checkModel = LoadModelFromFile(checkModelFile);
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to get CHECK model: " + ex.Message, ex);
            }

            s_logger.Trace("Building diff");
            ModelComparison diff = new ModelComparison(refModel, checkModel);

            string? diffMessage;
            if (diff.ModelsAreSame())
            {
                s_logger.Trace($"Models are same!");
                diffMessage = null;
            }
            else
            {
                diffMessage = 
@$"Models differ! 
Got {diff.entities_not_in_check_count} entitites not in CHECK model, 
{diff.entities_not_in_ref_count} entites not in REF model, 
{diff.entities_different_properties_count} entities with different properties";

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

        private static async Task PushModelToHttp(Model model, Uri pushUrl)
        {
            s_logger.Info("Pushing model to " + pushUrl);
            string modelJson = model.ToJson().ToFlatString(new SerializationSettings() { SerializeNullProperties = false });
            try
            {
                using (HttpContent content = new StringContent(modelJson, new MediaTypeHeaderValue("application/json")))
                using (HttpClient httpClient = new HttpClient())
                {
                    HttpResponseMessage response = await httpClient.PostAsync(pushUrl, content);
                    if (!response.IsSuccessStatusCode)
                    {
                        string responseMsg;
                        try
                        {
                            responseMsg = await response.Content.ReadAsStringAsync();
                        }
                        catch (Exception ex)
                        {
                            responseMsg = $"(Failed to read response message: {ex.Message})";
                        }
                        throw new Exception($"Got unexpected response code {(int)response.StatusCode} {response.StatusCode}: '{responseMsg}'");
                    }

                    s_logger.Info("done");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to push model: {ex.Message}", ex);
            }
        }

        private static Task SaveModelToFile(Model model, string outputFile)
        {
            s_logger.Info("Writing model to " + outputFile);
            return File.WriteAllTextAsync(outputFile, model.ToJson().ToIndentedString(new SerializationSettings() {SerializeNullProperties = false }));
        }

        private static async Task<Model> ExtractModel(string specFile)
        {
            s_logger.Info("Reading model spec file " + specFile);
            ModelSpec modelSpec = ModelSpec.LoadFromFile(specFile, s_registry);

            Model model;

            //TODO: use cli args to specify tracing file
            using (StreamWriter traceFile = File.CreateText("./trace.txt"))
            {
                modelSpec.OnTrace += (string line) => {
                    traceFile.WriteLine("====");
                    traceFile.WriteLine(line);
                };

                s_logger.Info("Retrieving model " + modelSpec.Name);
                try
                {
                    model = await modelSpec.GetModelAsync();
                }
                catch (Exception ex)
                {
                    traceFile.WriteLine("====");
                    traceFile.WriteLine(ex.Message);
                    throw;
                }
                s_logger.Info($"Got {model.entities?.Count ?? 0} entities, and {model.links?.Count ?? 0} links. Invalid objects count: {model.invalid_objects?.Count}");

            }
            return model;
        }

        private static Model LoadModelFromFile(string modelFile)
        {
            s_logger.Trace("Loading model from " + modelFile);
            using (Stream stream = File.OpenRead(modelFile))
            {
                return Model.FromJson(stream);
            }
        }

        private static async Task SaveDiffToFile(ModelComparison diff, string file)
        {
            s_logger.Trace("Saving diff to " + file);
            JsonSerializerOptions serializerOptions = new JsonSerializerOptions() {
                WriteIndented = true
            };
            serializerOptions.Converters.Add(new JsonStringEnumConverter());

            using (Stream stream = File.Create(file))
            {
                await JsonSerializer.SerializeAsync(stream, diff, serializerOptions);
            }
        }
    }
}