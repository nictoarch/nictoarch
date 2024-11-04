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
    internal sealed class YamlnplaceNodeDeserializer : INodeDeserializer
    {
        internal const string TAG = "!inplace";

        private readonly Dictionary<string, string> m_fileCache = new Dictionary<string, string>();
        private readonly Dictionary<string, JToken> m_jsonCache = new Dictionary<string, JToken>();
        private readonly string m_basePath;

        public YamlnplaceNodeDeserializer(string basePath)
        {
            this.m_basePath = basePath;
        }

        bool INodeDeserializer.Deserialize(IParser parser, Type expectedType, Func<IParser, Type, object?> nestedObjectDeserializer, out object? value, ObjectDeserializer rootDeserializer)
        {
            if (parser.Accept<Scalar>(out Scalar? scalar) && scalar.Tag == TAG)
            {
                parser.MoveNext();  //consume scalar

                string filePath = scalar.Value;
                JsonataQuery? query;
                int separatorIndex = filePath.IndexOf('#');
                if (separatorIndex >= 0)
                {
                    string queryPath = filePath.Substring(separatorIndex + 1);
                    filePath = filePath.Substring(0, separatorIndex);
                    try
                    {
                        query = new JsonataQuery(queryPath);
                    }
                    catch (Exception ex)
                    {
                        throw new YamlException(scalar.Start, scalar.End, $"Failed to parse query part of {TAG} ({queryPath}): {ex.Message}", ex);
                    }
                }
                else
                {
                    query = null;
                }

                if (String.IsNullOrWhiteSpace(filePath))
                {
                    throw new YamlException(scalar.Start!, scalar.End!, $"{TAG} value should be in format '<filename>[#path]', provided: '{scalar.Value}'");
                }
                filePath = Path.Combine(this.m_basePath, filePath);

                if (!this.m_fileCache.TryGetValue(filePath, out string? fileContent))
                {
                    try
                    {
                        fileContent = File.ReadAllText(filePath);
                        this.m_fileCache.Add(filePath, fileContent);
                    }
                    catch (Exception ex)
                    {
                        throw new YamlException(scalar.Start, scalar.End, $"Failed to read content of {TAG} file {filePath}: {ex.Message}", ex);
                    }
                }

                if (query == null)
                {
                    //if no query specified, return whole file content as string.
                    value = fileContent.Trim();
                    return true;
                }
                
                if (!this.m_jsonCache.TryGetValue(filePath, out JToken? json))
                {
                    //see https://github.com/aaubry/YamlDotNet/blob/master/YamlDotNet.Samples/ConvertYamlToJson.cs
                    object? yamlObject;
                    try
                    {
                        IDeserializer deserializer = new DeserializerBuilder().Build();
                        using (StringReader reader = new StringReader(fileContent))
                        {
                            yamlObject = deserializer.Deserialize(reader);
                        }
                    }
                    catch (Exception ex)
                    {
                        throw new YamlException(scalar.Start, scalar.End, $"Failed to parse yaml content of {TAG} file {filePath}: {ex.ToString()}", ex);
                    }

                    ISerializer serializer = new SerializerBuilder()
                            .JsonCompatible()
                            .Build();

                    string jsonStr = serializer.Serialize(yamlObject);

                    try
                    {
                        json = JToken.Parse(jsonStr);
                        this.m_jsonCache.Add(filePath, json);
                    }
                    catch (Exception ex)
                    {
                        throw new YamlException(scalar.Start, scalar.End, $"Failed to parse json-converted yaml content of {TAG} file {filePath}: {ex.Message}", ex);
                    }
                }

                try
                {
                    JToken queryResult = query.Eval(json);
                    switch (queryResult.Type)
                    {
                    case JTokenType.Null:
                        value = null;
                        return true;
                    case JTokenType.String:
                        value = (string)queryResult;
                        return true;
                    case JTokenType.Integer:
                    case JTokenType.Boolean:
                    case JTokenType.Float:
                        value = (string)queryResult;
                        return true;
                    default:
                        throw new Exception($"Query returned {queryResult.Type} while expected a value");
                    }
                }
                catch (Exception ex)
                {
                    throw new YamlException(scalar.Start, scalar.End, $"Failed to execute {TAG} query '{query}' on file {filePath}: {ex.Message}", ex);
                }
            }

            value = null;
            return false;
        }
    }
}
