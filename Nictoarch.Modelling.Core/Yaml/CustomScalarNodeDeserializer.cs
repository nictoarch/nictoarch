using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jsonata.Net.Native;
using Jsonata.Net.Native.Json;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;
using YamlDotNet;
using System.Reflection;

namespace Nictoarch.Modelling.Core.Yaml
{
    internal sealed class CustomScalarNodeDeserializer : CustomNodeDeserializer
    {
        internal const string TAG_ENV = "!env";
        internal const string TAG_INPLACE = "!inplace";

        private const string PARSE_FACTORY_NAME = "Parse";

        private readonly string m_basePath;
        private readonly Dictionary<string, string> m_fileCache = new Dictionary<string, string>();
        private readonly Dictionary<string, JToken> m_jsonCache = new Dictionary<string, JToken>();

        internal sealed class TagTypeResolver : INodeTypeResolver
        {
            bool INodeTypeResolver.Resolve(NodeEvent? nodeEvent, ref Type currentType)
            {
                if (nodeEvent is Scalar 
                    && (nodeEvent.Tag == TAG_ENV || nodeEvent.Tag == TAG_INPLACE)
                )
                {
                    //will result in CustomScalarNodeDeserializer.Deserialize getting proper expectedType for tags
                    // see https://github.com/aaubry/YamlDotNet/issues/909#issuecomment-2007242473
                    return true;
                }
                return false;
            }
        }

        public CustomScalarNodeDeserializer(INodeDeserializer internalDeserialzier, ModelSpecObjectFactory objectFactory, string basePath) 
            : base(internalDeserialzier, objectFactory)
        {
            this.m_basePath = basePath;
        }

        public override bool Deserialize(IParser parser, Type expectedType, Func<IParser, Type, object?> nestedObjectDeserializer, out object? value, ObjectDeserializer rootDeserializer)
        {
            if (parser.Current is not YamlDotNet.Core.Events.Scalar scalar)
            {
                value = null;
                return false;
            }

            //see https://github.com/aaubry/YamlDotNet/issues/368
            // https://dotnetfiddle.net/Q2HCSY

            if (scalar.Tag == TAG_ENV)
            {
                parser.MoveNext();  //consume scalar
                value = this.GetEnvValue(scalar.Value, expectedType, scalar);
                value = this.PostProcessCustomObject(value, expectedType);
            }
            else if (scalar.Tag == TAG_INPLACE)
            {
                parser.MoveNext();  //consume scalar
                value = this.GetInplaceValue(scalar.Value, expectedType, scalar);
                value = this.PostProcessCustomObject(value, expectedType);
            }
            else if (!this.m_nodeDeserializer.Deserialize(parser, expectedType, nestedObjectDeserializer, out value, rootDeserializer))
            {
                return false;
            }

            this.PostProcessDeserializedObject(value, scalar);

            return true;
        }

        private object? PostProcessCustomObject(object? value, Type expectedType)
        {
            //this is done in YamlDotNet.Serialization.Utilities.TypeConverter
            if (value is string strValue)
            {
                MethodInfo? factoryMethod = GetPublicStaticMethod(expectedType, PARSE_FACTORY_NAME, typeof(string));
                if (factoryMethod != null)
                {
                    value = factoryMethod.Invoke(null, [value]);
                }
            }
            return value;
        }

        //see of YamlDotNet.ReflectionExtensions.GetPublicStaticMethod
        private static MethodInfo? GetPublicStaticMethod(Type type, string name, params Type[] parameterTypes)
        {
            return type.GetRuntimeMethods()
                .FirstOrDefault(m => {
                    if (m.IsPublic && m.IsStatic && m.Name.Equals(name))
                    {
                        ParameterInfo[] parameters = m.GetParameters();
                        return parameters.Length == parameterTypes.Length
                            && parameters.Zip(parameterTypes, (pi, pt) => pi.ParameterType == pt).All(r => r);
                    }
                    return false;
                });
        }

        private object? GetEnvValue(string varName, Type expectedType, ParsingEvent parsingEvent)
        {
            if (String.IsNullOrWhiteSpace(varName))
            {
                throw new YamlException(parsingEvent.Start!, parsingEvent.End!, $"{TAG_ENV} value should be an string (env variable name), provided: '{varName}'");
            }
            string? varValue = Environment.GetEnvironmentVariable(varName);
            if (String.IsNullOrWhiteSpace(varValue))
            {
                throw new YamlException(parsingEvent.Start!, parsingEvent.End!, $"Failed to find an environment variable with name '{varName}'");
            }

            return varValue;
        }

        private object? GetInplaceValue(string filePath, Type expectedType, ParsingEvent parsingEvent)
        {
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
                    throw new YamlException(parsingEvent.Start, parsingEvent.End, $"Failed to parse query part of {TAG_INPLACE} ({queryPath}): {ex.Message}", ex);
                }
            }
            else
            {
                query = null;
            }

            if (String.IsNullOrWhiteSpace(filePath))
            {
                throw new YamlException(parsingEvent.Start, parsingEvent.End, $"{TAG_INPLACE} value should be in format '<filename>[#path]', provided: '{filePath}'");
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
                    throw new YamlException(parsingEvent.Start, parsingEvent.End, $"Failed to read content of {TAG_INPLACE} file {filePath}: {ex.Message}", ex);
                }
            }

            if (query == null)
            {
                //if no query specified, return whole file content as string.
                return fileContent.Trim();
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
                    throw new YamlException(parsingEvent.Start, parsingEvent.End, $"Failed to parse yaml content of {TAG_INPLACE} file {filePath}: {ex.ToString()}", ex);
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
                    throw new YamlException(parsingEvent.Start, parsingEvent.End, $"Failed to parse json-converted yaml content of {TAG_INPLACE} file {filePath}: {ex.Message}", ex);
                }
            }

            try
            {
                JToken queryResult = query.Eval(json);
                switch (queryResult.Type)
                {
                case JTokenType.Null:
                    return null;
                case JTokenType.String:
                    return (string)queryResult;
                case JTokenType.Integer:
                case JTokenType.Boolean:
                case JTokenType.Float:
                    return (string)queryResult;
                default:
                    throw new Exception($"Query returned {queryResult.Type} while expected a value");
                }
            }
            catch (Exception ex)
            {
                throw new YamlException(parsingEvent.Start, parsingEvent.End, $"Failed to execute {TAG_INPLACE} query '{query}' on file {filePath}: {ex.Message}", ex);
            }
        }
    }
}
