using BtmsGateway.Services.Converter;
using FluentAssertions;

namespace BtmsGateway.Test.Services.Converter;

public class XmlToJsonConverterTests
{
    [Theory]
    [ClassData(typeof(XmlToJsonTestData))]
    public void When_receiving_valid_xml_Then_should_convert_to_json(string because, string xml, string expectedJson)
    {
        XmlToJsonConverter.Convert(xml).Should().Be(expectedJson, because);
    }

    [Fact]
    public void When_receiving_invalid_xml_Then_should_fail()
    {
        var act = () => XmlToJsonConverter.Convert("<root><not-root>");
        act.Should().Throw<ArgumentException>();
    }

    private class XmlToJsonTestData : TheoryData<string, string, string>
    {
        public XmlToJsonTestData()
        {
            Add("Simple self-closing tag", XmlSimpleSelfClosing, JsonSimpleEmpty);
            Add("Simple self-closing tag w/ space", XmlSimpleSelfClosingWithSpace, JsonSimpleEmpty);
            Add("Simple empty tag", XmlSimpleEmpty, JsonSimpleEmpty);
            Add("Simple content tag", XmlSimpleContent, JsonSimpleContent);
            Add("Complex single layer", XmlComplexSingleLevel, JsonComplexSingleLevel);
            Add("Complex multi layer", XmlComplexMultiLevel, JsonComplexMultiLevel);
        }

        private const string XmlSimpleSelfClosing = "<root/>";
        private const string XmlSimpleSelfClosingWithSpace = "<root />";
        private const string XmlSimpleEmpty = "<root></root>";
        private const string XmlSimpleContent = "<root>data</root>";
        private const string XmlComplexSingleLevel = """
                                                     <root>
                                                       <tag1>data1</tag1>
                                                       <tag2>data2</tag2>
                                                     </root>
                                                     """;
        private const string XmlComplexMultiLevel = """
                                                    <root>
                                                      <element1>
                                                        <tag1>data1</tag1>
                                                        <tag2>data2</tag2>
                                                      </element1>
                                                      <element2>
                                                        <tag3>data3</tag3>
                                                        <element3>
                                                          <tag4/>
                                                          <tag5></tag5>
                                                        </element3>
                                                      </element2>
                                                    </root>
                                                    """;

        private static readonly string JsonSimpleEmpty = """
                                                         {
                                                           "root": null
                                                         }
                                                         """.Replace("\r\n", "\n");
        private static readonly string JsonSimpleContent = """
                                                           {
                                                             "root": "data"
                                                           }
                                                           """.Replace("\r\n", "\n");
        private static readonly string JsonComplexSingleLevel = """
                                                                {
                                                                  "root": {
                                                                    "tag1": "data1",
                                                                    "tag2": "data2"
                                                                  }
                                                                }
                                                                """.Replace("\r\n", "\n");
        private static readonly string JsonComplexMultiLevel = """
                                                               {
                                                                 "root": {
                                                                   "element1": {
                                                                     "tag1": "data1",
                                                                     "tag2": "data2"
                                                                   },
                                                                   "element2": {
                                                                     "tag3": "data3",
                                                                     "element3": {
                                                                       "tag4": null,
                                                                       "tag5": null
                                                                     }
                                                                   }
                                                                 }
                                                               }
                                                               """.Replace("\r\n", "\n");
    }
}