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
using YamlDotNet.RepresentationModel;
using gfs.YamlDotNet.YamlPath;

namespace Nictoarch.Modelling.Core.Yaml
{
    internal sealed class CustomScalarNodeDeserializer : CustomNodeDeserializer
    {
        internal const string TAG_ENV = "!env";
        internal const string TAG_INPLACE = "!inplace";

        private const string PARSE_FACTORY_NAME = "Parse";

        private readonly string m_basePath;
        private readonly DeserializerBuilder m_deserializerBuilder;
        private readonly Dictionary<string, YamlNode> m_yamlCache = new Dictionary<string, YamlNode>();

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

        public CustomScalarNodeDeserializer(INodeDeserializer internalDeserialzier, ModelSpecObjectFactory objectFactory, string basePath, DeserializerBuilder deserializerBuilder) 
            : base(internalDeserialzier, objectFactory)
        {
            this.m_basePath = basePath;
            this.m_deserializerBuilder = deserializerBuilder;
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
            try
            {
                return this.GetInplaceValueInternal(filePath, expectedType);
            }
            catch (Exception ex)
            {
                throw new YamlException(parsingEvent.Start, parsingEvent.End, $"Failed to process {TAG_INPLACE} ({filePath}): {ex.Message}", ex);
            }
        }

        private object? GetInplaceValueInternal(string filePath, Type expectedType)
        {
            string? queryPath;
            {
                int separatorIndex = filePath.IndexOf('#');
                if (separatorIndex >= 0)
                {
                    queryPath = filePath.Substring(separatorIndex + 1);
                    filePath = filePath.Substring(0, separatorIndex);
                }
                else
                {
                    queryPath = null;
                }
            }

            if (String.IsNullOrWhiteSpace(filePath))
            {
                throw new Exception($"Value should be in format '<filename>[#path]', provided: '{filePath}'");
            }
            filePath = Path.Combine(this.m_basePath, filePath);

            if (!this.m_yamlCache.TryGetValue(filePath, out YamlNode? yamlNode))
            {
                using (StreamReader reader = new StreamReader(filePath))
                {
                    YamlStream stream = new YamlStream();
                    try
                    {
                        stream.Load(reader);
                    }
                    catch (Exception ex)
                    {
                        throw new Exception($"Failed to parse YAML file '{filePath}': {ex.Message}", ex);
                    }

                    yamlNode = stream.Documents[0].RootNode;
                    this.m_yamlCache.Add(filePath, yamlNode);
                }
            }

            
            if (queryPath != null)
            {
                List<YamlNode> nodes = yamlNode.Query(queryPath).ToList();
                if (nodes.Count != 1)
                {
                    throw new Exception($"Yaml path expression '{queryPath}' resulted in {nodes.Count} nodes, while it should return just a single one");
                }
                yamlNode = nodes[0];
            }

            IParser parser = yamlNode.ConvertToEventStream().ConvertToParser();

            IDeserializer deserializer = this.m_deserializerBuilder.Build(); //TODO: use filePath as a new basePath here

            try
            {
                object? value = deserializer.Deserialize(parser, expectedType);
                return value;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to deserialize content file {filePath} into {expectedType.Name}: {ex.Message}", ex);
            }
        }
    }
}
