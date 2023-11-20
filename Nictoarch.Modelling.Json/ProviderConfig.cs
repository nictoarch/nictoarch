using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Jsonata.Net.Native;
using Nictoarch.Modelling.Core.Yaml;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;

namespace Nictoarch.Modelling.Json
{
    public sealed class ProviderConfig: ModelSpecImpl.SourceBase
    {
        public enum ESourceTransform
        {
            none,
            xml2json
        }

        public abstract class Auth
        {
            [Required] public string type { get; set; } = default!;

            internal abstract AuthenticationHeaderValue? CreateHeader();
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
        [Required] public ESourceTransform source_transform { get; set; } = ESourceTransform.none;
    }
}
