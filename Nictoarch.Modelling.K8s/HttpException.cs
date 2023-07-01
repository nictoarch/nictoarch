using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Nictoarch.Modelling.K8s
{
    internal sealed class HttpException: Exception
    {
        internal readonly HttpStatusCode StatusCode;

        internal HttpException(string message, HttpStatusCode statusCode) 
            : base(message) 
        { 
            this.StatusCode = statusCode;
        }
        
    }
}
