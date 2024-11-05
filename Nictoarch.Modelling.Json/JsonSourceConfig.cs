using System;
using System.ComponentModel.DataAnnotations;
using System.Net.Http.Headers;
using System.Text;
using Nictoarch.Modelling.Core;
using Nictoarch.Modelling.Core.Spec;
using Nictoarch.Modelling.Core.Yaml;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;

namespace Nictoarch.Modelling.Json
{
    public sealed class JsonSourceConfig: SourceConfigBase, IYamlOnDeserialized
    {
        public FileSource? file { get; set; }
        public HttpSource? http { get; set; }
        public InplaceSource? inplace { get; set; }

        public void OnDeserialized(ParsingEvent parsingEvent)
        {
            int sourcesCount = 0;
            if (this.file != null)
            {
                ++sourcesCount;
            }
            if (this.http != null)
            {
                ++sourcesCount;
            }
            if (this.inplace != null)
            {
                ++sourcesCount;
            }

            if (sourcesCount == 0)
            {
                throw new YamlException(parsingEvent.Start, parsingEvent.End, $"For json source a data source should be specified: {nameof(this.file)}, {nameof(this.http)}, {nameof(this.inplace)}");
            }
            else if (sourcesCount > 1)
            {
                throw new YamlException(parsingEvent.Start, parsingEvent.End, $"For json source only one data source should be specified: {nameof(this.file)}, {nameof(this.http)}, {nameof(this.inplace)}");
            }
        }

        public sealed class FileSource: IMaybeYamlSimpleValue
        {
            public BasePathAutoProperty base_path { get; set; } = default!; //automatically provided by the ModelSpecObjectFactory
            [Required, YamlSimpleValue] public string path { get; set; } = default!;
        }

        public sealed class HttpSource: IMaybeYamlSimpleValue
        {
            public Auth? auth { get; set; }
            [Required, YamlSimpleValue] public string url { get; set; } = default!;
        }

        public sealed class InplaceSource: IMaybeYamlSimpleValue
        {
            [Required, YamlSimpleValue] public string value { get; set; } = default!;
        }

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
    }
}
