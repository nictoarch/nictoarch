using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Jsonata.Net.Native.Json;
using Nictoarch.Common.Xml2Json;
using Nictoarch.Modelling.Core;
using Nictoarch.Modelling.Core.Elements;

namespace Nictoarch.Modelling.Json
{
    /*
    public sealed class JsonModelProvider : IModelProvider<QuerySelector, QuerySelector>
    {
        private readonly JToken m_json;

        internal JsonModelProvider(JToken json)
        {
            this.m_json = json;
        }

        void IDisposable.Dispose()
        {
            //nothing to od
        }

        Task<List<Entity>> IModelProvider<QuerySelector, QuerySelector>.GetEntitiesAsync(QuerySelector spec, CancellationToken cancellationToken)
        {
            JToken resultToken;
            try
            {
                resultToken = spec.entityQuery.Eval(this.m_json);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to execure JSONata query: {ex.Message}", ex);
            }

            if (resultToken.Type != JTokenType.Array)
            {
                throw new Exception($"JSONata query for entity selector should return array (of entities), but it returned {resultToken.Type}:\n{resultToken.ToIndentedString()}");
            }

            cancellationToken.ThrowIfCancellationRequested();

            JArray resultArray = (JArray)resultToken;
            List<Entity> entities = new List<Entity>(resultArray.Count);
            foreach (JToken token in resultArray.ChildrenTokens)
            {
                Entity entity = token.ToObject<Entity>();
                entity.Validate();
                entities.Add(entity);
                cancellationToken.ThrowIfCancellationRequested();
            }

            return Task.FromResult(entities);
        }

        Task<List<object>> IModelProvider<QuerySelector, QuerySelector>.GetInvalidObjectsAsync(QuerySelector spec, CancellationToken cancellationToken)
        {
            JToken resultToken;
            try
            {
                resultToken = spec.entityQuery.Eval(this.m_json);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to execure JSONata query: {ex.Message}", ex);
            }

            List<object> results = new List<object>();

            switch (resultToken.Type)
            {
            case JTokenType.Array:
                foreach (JToken result in ((JArray)resultToken).ChildrenTokens)
                {
                    results.Add(result);
                }
                break;
            case JTokenType.Object:
                results.Add(resultToken);
                break;
            case JTokenType.Null:
            case JTokenType.Undefined:
                //consider it empty result
                break;
            case JTokenType.Integer:
            case JTokenType.Float:
            case JTokenType.String:
            case JTokenType.Boolean:
            default:
                throw new Exception($"Invalid objects list query should return an Array or Object. It returned {resultToken.Type} ('{resultToken.ToFlatString()}')");
            }

            return Task.FromResult(results);
        }
    }
    */
}