using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Jsonata.Net.Native.Json;
using Nictoarch.Common.Xml2Json;
using Nictoarch.Modelling.Core.AppSupport;
using NLog;

namespace Nictoarch.Modelling.Json
{
    public sealed class JsonAppExtension : IAppExtension
    {
        private readonly Logger m_logger = LogManager.GetCurrentClassLogger();

        string IAppExtension.Name => "json";
        string IAppExtension.Description => "JSON and XML file processing commands";

        List<Command> IAppExtension.GetCommands()
        {
            List<Command> result = new List<Command>();

            {
                Command xml2JsonCommand = new Command("xml2json", "Convert XML file to JSON");
                xml2JsonCommand.AddAlias("xml");

                Argument<string> xmlFileNameArg = new Argument<string>("xml-file-name", "Source file name");
                xml2JsonCommand.Add(xmlFileNameArg);

                Option<string?> outputJsonFileName = new Option<string?>(new string[] { "--output", "-o" }, () => null, $"Output JSON file name. If not specified, will use '<{xmlFileNameArg.Name}>.json'");
                xml2JsonCommand.Add(outputJsonFileName);

                xml2JsonCommand.SetHandler(
                    Xml2JsonCommand,
                    xmlFileNameArg, outputJsonFileName
                );

                result.Add(xml2JsonCommand);
            }

            return result;
        }

        private async Task Xml2JsonCommand(string xmlFileName, string? outputFileName)
        {
            Xml2JsonConverter converter = new Xml2JsonConverter();

            this.m_logger.Trace("Loading file " + xmlFileName);
            string xml = await File.ReadAllTextAsync(xmlFileName);

            this.m_logger.Trace("Converting to JSON");
            JObject json = converter.Convert(xml);

            outputFileName ??= xmlFileName + ".json";
            this.m_logger.Trace("Writing to " + outputFileName);
            await File.WriteAllTextAsync(outputFileName, json.ToIndentedString());
        }
    }
}
