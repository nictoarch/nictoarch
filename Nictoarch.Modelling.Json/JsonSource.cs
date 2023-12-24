using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Jsonata.Net.Native.Json;
using Nictoarch.Common.Xml2Json;
using Nictoarch.Modelling.Core;

namespace Nictoarch.Modelling.Json
{
    public sealed class JsonSource : ISource<JsonExtractConfig>
    {
        private readonly string m_sourceDataString;

        //caching
        private JsonExtractConfig.ESourceTransform? m_transform = null;
        private JToken? m_extractedData = null;

        public JsonSource(string sourceDataString)
        {
            this.m_sourceDataString = sourceDataString;
        }

        ValueTask IAsyncDisposable.DisposeAsync()
        {
            //nothing to do
            return ValueTask.CompletedTask;
        }

        Task<JToken> ISource<JsonExtractConfig>.Extract(JsonExtractConfig extractConfig, CancellationToken cancellationToken)
        {
            if (extractConfig.transform != this.m_transform || this.m_extractedData == null)
            {
                this.m_transform = extractConfig.transform;
                switch (this.m_transform.Value)
                {
                case JsonExtractConfig.ESourceTransform.none:
                    this.m_extractedData = JToken.Parse(this.m_sourceDataString);
                    break;
                case JsonExtractConfig.ESourceTransform.xml2json:
                    Xml2JsonConverter converter = new Xml2JsonConverter();
                    this.m_extractedData = converter.Convert(this.m_sourceDataString, cancellationToken);
                    break;
                default:
                    throw new Exception("Unexpected transform type: " + this.m_transform.Value.ToString());
                }
            }

            return Task.FromResult(this.m_extractedData);
        }
    }
}
