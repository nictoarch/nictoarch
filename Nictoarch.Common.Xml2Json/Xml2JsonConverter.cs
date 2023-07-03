using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Jsonata.Net.Native.Json;

namespace Nictoarch.Common.Xml2Json
{
    public sealed class Xml2JsonConverter
    {
        public sealed class Settings
        {
            public bool KeepNamespacesInNames { get; set; } = true;
            public string NamePropertyName { get; set; } = "_type";
            public string NestedPropertyName { get; set; } = "_nested";
            public string ValuePropertyName { get; set; } = "_value";
        }

        private readonly Settings m_settings;

        public Xml2JsonConverter(Settings? settings = null)
        {
            this.m_settings = settings ?? new Settings();
        }

        public JObject Convert(string xml)
        {
            XDocument doc = XDocument.Parse(xml);
            return this.Convert(doc);
        }

        public JObject Convert(XDocument doc)
        {
            return this.Convert(doc.Root!)!;
        }

        public JObject Convert(XElement element)
        {
            JObject result = new JObject();
            string name;
            if (this.m_settings.KeepNamespacesInNames)
            {
                name = element.Name.ToString();
            }
            else
            {
                name = element.Name.LocalName;
            }
            result.Add(this.m_settings.NamePropertyName, new JValue(name));
            foreach (XAttribute attribute in element.Attributes())
            {
                result.Add(attribute.Name.ToString(), attribute.Value == null ? JValue.CreateNull() : new JValue(attribute.Value));
            }

            if (!element.IsEmpty)
            {
                string? value = GetElementShallowValue(element);
                if (value != null)
                {
                    result.Add(this.m_settings.ValuePropertyName, new JValue(value));
                }
            }

            if (element.HasElements)
            {
                JArray nested = new JArray();
                foreach (XElement childElement in element.Elements())
                {
                    JObject child = this.Convert(childElement);
                    nested.Add(child);
                }
                result.Add(this.m_settings.NestedPropertyName, nested);
            }
            return result;
        }


        //see https://learn.microsoft.com/en-us/dotnet/standard/linq/retrieve-shallow-value-element
        private static string? GetElementShallowValue(XElement element)
        {
            bool matchedSomething = false;
            StringBuilder builder = new StringBuilder();
            foreach (XText text in element.Nodes().OfType<XText>())
            {
                builder.Append(text.Value);
                matchedSomething = true;
            }

            if (matchedSomething)
            {
                return builder.ToString()
                    .Trim();
            }
            else
            {
                return null;
            }
        }
    }
}