using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jsonata.Net.Native;
using Jsonata.Net.Native.Json;

namespace Nictoarch.Modelling.Core.Spec
{
    internal static class JsonataHelpers
    {
        public static JToken Eval(this JsonataQuery query, JToken data, string queryLabel) 
        {
            try
            {
                JToken result = query.Eval(data);
                return result;
            }
            catch (Exception ex)
            {
                throw new JsonataEvalException($"Failed to execute query '{queryLabel}': {ex.Message}.", query, data, ex);
            }
        }
    }
}
