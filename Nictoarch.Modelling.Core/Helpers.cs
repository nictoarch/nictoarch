using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nictoarch.Modelling.Core
{
    internal static class Helpers
    {
        public static string JoinInnerMessages(this Exception ex, string separator = " --> ")
        {
            return ex.InnerException == null
                    ? ex.Message
                    : ex.Message + separator + ex.InnerException.JoinInnerMessages(separator);
        }
    }
}
