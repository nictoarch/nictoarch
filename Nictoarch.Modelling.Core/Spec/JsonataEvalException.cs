using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jsonata.Net.Native;
using Jsonata.Net.Native.Json;

namespace Nictoarch.Modelling.Core.Spec
{
    public sealed class JsonataEvalException: Exception
    {
        public JsonataQuery Query { get; }
        public JToken SourceData { get; }

        public static string FormatMessage(string message, JsonataQuery query, JToken sourceData)
        {
            return $"{message}\n---\nquery:\n{query}\n---\ndata:\n{sourceData.ToIndentedString()}";
        }

        public JsonataEvalException(string message, JsonataQuery query, JToken sourceData, Exception? innerException = null) 
            :base(FormatMessage(message, query, sourceData), innerException)
        {
            this.Query = query;
            this.SourceData = sourceData;
        }
    }
}
