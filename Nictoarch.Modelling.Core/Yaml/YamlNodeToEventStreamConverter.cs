using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.RepresentationModel;

namespace Nictoarch.Modelling.Core.Yaml
{
    // see https://stackoverflow.com/a/40727087/376066
    public static class YamlNodeToEventStreamConverter
    {
        public static IEnumerable<ParsingEvent> ConvertToEventStream(this YamlStream stream)
        {
            yield return new StreamStart();
            foreach (YamlDocument document in stream.Documents)
            {
                foreach (ParsingEvent evt in ConvertToEventStream(document))
                {
                    yield return evt;
                }
            }
            yield return new StreamEnd();
        }

        public static IEnumerable<ParsingEvent> ConvertToEventStream(this YamlDocument document)
        {
            yield return new DocumentStart();
            foreach (ParsingEvent evt in ConvertToEventStream(document.RootNode))
            {
                yield return evt;
            }
            yield return new DocumentEnd(false);
        }

        public static IEnumerable<ParsingEvent> ConvertToEventStream(this YamlNode node)
        {
            YamlScalarNode? scalar = node as YamlScalarNode;
            if (scalar != null)
            {
                return ConvertToEventStream(scalar);
            }

            YamlSequenceNode? sequence = node as YamlSequenceNode;
            if (sequence != null)
            {
                return ConvertToEventStream(sequence);
            }

            YamlMappingNode? mapping = node as YamlMappingNode;
            if (mapping != null)
            {
                return ConvertToEventStream(mapping);
            }

            throw new NotSupportedException(String.Format("Unsupported node type: {0}", node.GetType().Name));
        }

        private static IEnumerable<ParsingEvent> ConvertToEventStream(YamlScalarNode scalar)
        {
            yield return new Scalar(scalar.Anchor, scalar.Tag, scalar.Value!, scalar.Style, false, false);
        }

        private static IEnumerable<ParsingEvent> ConvertToEventStream(YamlSequenceNode sequence)
        {
            yield return new SequenceStart(sequence.Anchor, sequence.Tag, false, sequence.Style);
            foreach (YamlNode node in sequence.Children)
            {
                foreach (ParsingEvent evt in ConvertToEventStream(node))
                {
                    yield return evt;
                }
            }
            yield return new SequenceEnd();
        }

        private static IEnumerable<ParsingEvent> ConvertToEventStream(YamlMappingNode mapping)
        {
            yield return new MappingStart(mapping.Anchor, mapping.Tag, false, mapping.Style);
            foreach (KeyValuePair<YamlNode, YamlNode> pair in mapping.Children)
            {
                foreach (ParsingEvent evt in ConvertToEventStream(pair.Key))
                {
                    yield return evt;
                }
                foreach (ParsingEvent evt in ConvertToEventStream(pair.Value))
                {
                    yield return evt;
                }
            }
            yield return new MappingEnd();
        }

        public static IParser ConvertToParser(this IEnumerable<ParsingEvent> events)
        {
            return new EventStreamParserAdapter(events);
        }

        private sealed class EventStreamParserAdapter : IParser
        {
            private readonly IEnumerator<ParsingEvent> m_enumerator;

            public EventStreamParserAdapter(IEnumerable<ParsingEvent> events)
            {
                this.m_enumerator = events.GetEnumerator();
            }

            public ParsingEvent Current => this.m_enumerator.Current;

            public bool MoveNext()
            {
                return this.m_enumerator.MoveNext();
            }
        }
    }
}
