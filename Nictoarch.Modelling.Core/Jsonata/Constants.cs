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
        internal static readonly SerializationSettings NO_NULLS = new SerializationSettings() {
            SerializeNullProperties = false
        };

        internal static readonly ToObjectSettings ALLOW_MISSING = new ToObjectSettings() {
            AllowMissingProperties = true
        };
    }
}
