using System;
using System.ComponentModel.DataAnnotations;
using System.Net.Http.Headers;
using System.Text;
using Nictoarch.Modelling.Core;

namespace Nictoarch.Modelling.Json
{
    public sealed class JsonSourceConfig: ModelSpec.SourceConfigBase
    {
        public abstract class Auth
        {
            [Required] public string type { get; set; } = default!;

            internal abstract AuthenticationHeaderValue? CreateHeader();

            //used for short form "auth: none"
            public static Auth Parse(string v)
            {
                if (v == NoneAuth.TYPE)
                {
                    return new NoneAuth();
                }
                else
                {
                    throw new ArgumentException("Unexpected value " + v);
                }
            }
        }

        public sealed class NoneAuth : Auth
        {
            public const string TYPE = "none";
            
            internal override AuthenticationHeaderValue? CreateHeader() 
            {
                return null; 
            }
        }

        public sealed class BasicAuth: Auth
        {
            public const string TYPE = "basic";
            
            [Required] public string user { get; set; } = default!;
            [Required] public string pass { get; set; } = default!;

            internal override AuthenticationHeaderValue? CreateHeader()
            {
                return new AuthenticationHeaderValue(
                    scheme: "Basic",
                    parameter: Convert.ToBase64String(Encoding.ASCII.GetBytes(this.user + ":" + this.pass))
                );
            }
        }

        [Required] public string location { get; set; } = default!;
        public Auth? auth { get; set; }
    }
}
