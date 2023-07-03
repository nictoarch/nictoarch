using Nictoarch.Common;
using Nictoarch.Modelling.Core.Elements;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System;
using Nictoarch.Common.Xml2Json;
using System.IO;
using System.Xml.Linq;
using Jsonata.Net.Native.Json;

namespace Nictoarch.Modelling.Drawio.TestApp
{
    internal class Program
    {
        static Task Main(string[] args)
        {
            return ProgramHelper.MainWrapperAsync(() => MainInternal(args));
        }

        static async Task MainInternal(string[] args)
        {
            await ConvertXml2Json(args[0], args[1]);
        }

        static async Task ConvertXml2Json(string fromFile, string toFile)
        {
            Xml2JsonConverter converter = new Xml2JsonConverter();
            string xml = await File.ReadAllTextAsync(fromFile);
            JObject json = converter.Convert(xml);
            await File.WriteAllTextAsync(toFile, json.ToIndentedString());
        }
    }
}