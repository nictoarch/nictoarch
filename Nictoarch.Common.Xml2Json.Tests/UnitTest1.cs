using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Reflection;
using System;
using NUnit.Framework;
using System.Xml.Linq;
using Jsonata.Net.Native.Json;

namespace Nictoarch.Common.Xml2Json.Tests
{
    public class Tests
    {
        private const string TEST_SUITE_ROOT = "../../../data";

        [TestCaseSource(nameof(GetSimpleCases))]
        [TestCaseSource(nameof(GetTestCases))]
        public void TestConvertXml(string xml, string json)
        {
            XDocument xmlDoc;
            try
            {
                xmlDoc = XDocument.Parse(xml);
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to parse xml: " + ex.Message, ex);
            }

            JToken expectedJson;
            try
            {
                expectedJson = JToken.Parse(json);
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to paese json: " + ex.Message, ex);
            }

            Xml2JsonConverter converter = new Xml2JsonConverter();
            JToken convertedXml = converter.Convert(xmlDoc);
            string convertedXmlStr = convertedXml.ToIndentedString();
            Assert.IsTrue(expectedJson.DeepEquals(convertedXml), "Mismatch, got: \n" + convertedXmlStr);
        }

        //see https://www.xml.com/pub/a/2006/05/31/converting-between-xml-and-json.html
        public static IEnumerable<TestCaseData> GetSimpleCases()
        {
            yield return new TestCaseData("<e/>", "{'_type': 'e'}").SetName("simple.empty");
            yield return new TestCaseData("<e>text</e>", "{'_type': 'e', '_value': 'text'}").SetName("simple.value");
            yield return new TestCaseData("<e name='value' />", "{'_type': 'e', 'name': 'value'}").SetName("simple.attr");
            yield return new TestCaseData(
                    "<e name='value'>text</e>", 
                    @"{
                        '_type': 'e', 
                        'name': 'value', 
                        '_value': 'text'
                    }"
                ).SetName("simple.attr_value");
            yield return new TestCaseData(
                    @"<e> 
                        <a>text</a> 
                        <b>text</b> 
                    </e>",
                    @"{
                        '_type': 'e', 
                        '_nested': [
                            {
                                '_type': 'a',
                                '_value': 'text'
                            },
                            {
                                '_type': 'b',
                                '_value': 'text'
                            }
                        ]
                    }"
                ).SetName("simple.nested");
            yield return new TestCaseData(
                     @"<e> 
                        <a>text</a> 
                        <a>text</a> 
                    </e>",
                    @"{
                        '_type': 'e', 
                        '_nested': [
                            {
                                '_type': 'a',
                                '_value': 'text'
                            },
                            {
                                '_type': 'a',
                                '_value': 'text'
                            }
                        ]
                    }"
                ).SetName("simple.nested_same");
            yield return new TestCaseData(
                    @"<e> text <a>text</a> </e>",
                    @"{
                        '_type': 'e', 
                        '_value': 'text',
                        '_nested': [
                            {
                                '_type': 'a',
                                '_value': 'text'
                            },
                        ]
                    }"
                ).SetName("simple.mixed");
            yield return new TestCaseData(
                    @"
                        <e> 
	                        <a>text</a> 
	                        text 
	                        <a>text</a> 
                        </e>
                    ",
                    @"{
                        '_type': 'e', 
                        '_value': 'text',
                        '_nested': [
                            {
                                '_type': 'a',
                                '_value': 'text'
                            },
                            {
                                '_type': 'a',
                                '_value': 'text'
                            },
                        ]
                    }"
                ).SetName("simple.mixed2");

            //this one is adapted from https://www.oxygenxml.com/doc/versions/25.1/ug-editor/topics/convert-XML-to-JSON-x-tools.html
            yield return new TestCaseData( 
                    @"<p>This <b att='val'>is</b> an <b>example</b>!</p>",
                    @"{
                        '_type': 'p', 
                        '_value': 'This  an !',
                        '_nested': [
                            {
                                '_type': 'b',
                                'att': 'val',
                                '_value': 'is'
                            },
                            {
                                '_type': 'b',
                                '_value': 'example'
                            },
                        ]
                    }"
                ).SetName("simple.all");
        }

        public static List<TestCaseData> GetTestCases()
        {
            List<TestCaseData> results = new List<TestCaseData>();
            string casesDirectory = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, TEST_SUITE_ROOT);
            foreach (string xmlFile in Directory.EnumerateFiles(casesDirectory, "*.xml"))
            {
                string fileName = Path.GetFileNameWithoutExtension(xmlFile);
                //dot works like path separator in NUnit
                //string displayName = fileName.Replace(".", "_");
                string displayName = fileName;
                string xml = File.ReadAllText(xmlFile);
                string json = File.ReadAllText(Path.ChangeExtension(xmlFile, ".json"));

                TestCaseData data = new TestCaseData(xml, json);
                data.SetName(displayName);
                results.Add(data);
            }
            return results;
        }
    }
}