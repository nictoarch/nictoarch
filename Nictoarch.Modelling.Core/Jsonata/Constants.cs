using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jsonata.Net.Native.Json;

namespace Nictoarch.Modelling.Core.Jsonata
{
    internal static class Constants
    {
        internal static readonly SerializationOptions NO_NULLS = new SerializationOptions() {
            SerializeNullProperties = false
        };
    }
}
