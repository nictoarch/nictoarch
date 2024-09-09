using System;
using System.Collections.Generic;
using System.CommandLine.Parsing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jsonata.Net.Native;
using Jsonata.Net.Native.Json;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace Nictoarch.Modelling.Core.Yaml
{
    //see https://github.com/aaubry/YamlDotNet/issues/368
    // https://dotnetfiddle.net/Q2HCSY
    internal sealed class YamEnvNodeDeserializer : INodeDeserializer
    {
        internal const string TAG = "!env";

        public YamEnvNodeDeserializer()
        {
        }

        bool INodeDeserializer.Deserialize(IParser parser, Type expectedType, Func<IParser, Type, object?> nestedObjectDeserializer, out object? value, ObjectDeserializer rootDeserializer)
        {
            if (parser.Accept<Scalar>(out Scalar? scalar) && scalar.Tag == TAG)
            {
                parser.MoveNext();  //consume scalar

                string varName = scalar.Value;
                if (String.IsNullOrWhiteSpace(varName))
                {
                    throw new YamlException(scalar.Start!, scalar.End!, $"{TAG} value should be an string (env variable name), provided: '{scalar.Value}'");
                }
                string? varValue = Environment.GetEnvironmentVariable(varName);
                if (String.IsNullOrWhiteSpace(varValue))
                {
                    throw new YamlException(scalar.Start!, scalar.End!, $"Failed to find an environment variable with name '{varName}'");
                }

                value = varValue;
                return true;
            }

            value = null;
            return false;
        }
    }
}
