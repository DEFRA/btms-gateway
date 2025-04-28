using BtmsGateway.Test.TestUtils;

namespace BtmsGateway.Test.Services.Converter.Fixtures;

public class JsonToXmlTestData : TheoryData<string, string, string, string>
{
    public JsonToXmlTestData()
    {
        Add("Simple empty JSON", JsonEmpty, "Root", XmlEmptyRoot.LinuxLineEndings());
        Add("Simple null JSON property", JsonSimpleNullProperty, "Root", XmlSimpleNullElement.LinuxLineEndings());
        Add("Simple empty JSON property", JsonSimpleEmptyProperty, "Root", XmlSimpleEmptyElement.LinuxLineEndings());
        Add("Simple JSON property", JsonSimpleProperty, "Root", XmlSimpleElement.LinuxLineEndings());
        Add("Complex JSON single level", JsonComplexSingleLevel, "Root", XmlComplexSingleLevel.LinuxLineEndings());
        Add("Complex JSON multi level", JsonComplexMultiLevel, "Root", XmlComplexMultiLevel.LinuxLineEndings());
        Add(
            "Complex JSON multi level with multi item arrays",
            JsonComplexMultiLevelWithArrays,
            "Root",
            XmlComplexMultiLevelWithArrays.LinuxLineEndings()
        );
        Add(
            "Complex JSON multi level with single item arrays",
            JsonComplexMultiLevelWithSingleItemArrays,
            "Root",
            XmlComplexMultiLevelWithSingleItemArrays.LinuxLineEndings()
        );
    }

    private const string JsonEmpty = "{}";

    private const string JsonSimpleNullProperty = """
                                                {
                                                  "data": null
                                                }
                                                """;

    private const string JsonSimpleEmptyProperty = """
                                                 {
                                                   "data": ""
                                                 }
                                                 """;

    private const string JsonSimpleProperty = """
                                            {
                                              "data": "value1"
                                            }
                                            """;

    private const string JsonComplexSingleLevel = """
                                                {
                                                  "tag1": "data1",
                                                  "tag2": "data2"
                                                }
                                                """;

    private const string JsonComplexMultiLevel = """
                                               {
                                                 "element1": {
                                                   "tag1": "data1",
                                                   "tag2": 12.3
                                                 },
                                                 "element2": {
                                                   "tag3": true,
                                                   "element3": {
                                                     "tag4": null,
                                                     "tag5": "",
                                                     "tag6": 123,
                                                     "tag7": "abc\ndef\nghi"
                                                   }
                                                 }
                                               }
                                               """;

    private const string JsonComplexMultiLevelWithArrays = """
                                                         {
                                                           "items": [
                                                             {
                                                               "tag1": "dataA",
                                                               "tag2": 123
                                                             },
                                                             {
                                                               "tag1": "dataB",
                                                               "tag2": 456,
                                                               "documents": [
                                                                 {
                                                                   "tag3": "dataC",
                                                                   "tag4": "777"
                                                                 },
                                                                 {
                                                                   "tag3": "dataD",
                                                                   "tag4": "888"
                                                                 }
                                                               ],
                                                               "checks": [
                                                                 {
                                                                   "tag1": "dataD",
                                                                   "tag2": "dataE"
                                                                 }
                                                               ]
                                                             }
                                                           ]
                                                         }
                                                         """;

    private const string JsonComplexMultiLevelWithSingleItemArrays = """
                                                                   {
                                                                     "items": [
                                                                       {
                                                                         "tag1": "dataB",
                                                                         "tag2": 456,
                                                                         "documents": [
                                                                           {
                                                                             "tag3": "dataC",
                                                                             "tag4": "777"
                                                                           }
                                                                         ]
                                                                       }
                                                                     ]
                                                                   }
                                                                   """;

    private const string XmlEmptyRoot = """
                                      <?xml version="1.0" encoding="utf-8"?>
                                      <Root />
                                      """;

    private const string XmlSimpleNullElement = """
                                              <?xml version="1.0" encoding="utf-8"?>
                                              <Root>
                                                <Data>null</Data>
                                              </Root>
                                              """;

    private const string XmlSimpleEmptyElement = """
                                               <?xml version="1.0" encoding="utf-8"?>
                                               <Root>
                                                 <Data></Data>
                                               </Root>
                                               """;

    private const string XmlSimpleElement = """
                                          <?xml version="1.0" encoding="utf-8"?>
                                          <Root>
                                            <Data>value1</Data>
                                          </Root>
                                          """;

    private const string XmlComplexSingleLevel = """
                                                 <?xml version="1.0" encoding="utf-8"?>
                                                 <Root>
                                                   <Tag1>data1</Tag1>
                                                   <Tag2>data2</Tag2>
                                                 </Root>
                                                 """;

    private const string XmlComplexMultiLevel = """
                                                <?xml version="1.0" encoding="utf-8"?>
                                                <Root>
                                                  <Element1>
                                                    <Tag1>data1</Tag1>
                                                    <Tag2>12.3</Tag2>
                                                  </Element1>
                                                  <Element2>
                                                    <Tag3>True</Tag3>
                                                    <Element3>
                                                      <Tag4>null</Tag4>
                                                      <Tag5></Tag5>
                                                      <Tag6>123</Tag6>
                                                      <Tag7>abc
                                                def
                                                ghi</Tag7>
                                                    </Element3>
                                                  </Element2>
                                                </Root>
                                                """;

    private const string XmlComplexMultiLevelWithArrays = """
                                                          <?xml version="1.0" encoding="utf-8"?>
                                                          <Root>
                                                            <Item>
                                                              <Tag1>dataA</Tag1>
                                                              <Tag2>123</Tag2>
                                                            </Item>
                                                            <Item>
                                                              <Tag1>dataB</Tag1>
                                                              <Tag2>456</Tag2>
                                                              <Document>
                                                                <Tag3>dataC</Tag3>
                                                                <Tag4>777</Tag4>
                                                              </Document>
                                                              <Document>
                                                                <Tag3>dataD</Tag3>
                                                                <Tag4>888</Tag4>
                                                              </Document>
                                                              <Check>
                                                                <Tag1>dataD</Tag1>
                                                                <Tag2>dataE</Tag2>
                                                              </Check>
                                                            </Item>
                                                          </Root>
                                                          """;

    private const string XmlComplexMultiLevelWithSingleItemArrays = """
                                                                    <?xml version="1.0" encoding="utf-8"?>
                                                                    <Root>
                                                                      <Item>
                                                                        <Tag1>dataB</Tag1>
                                                                        <Tag2>456</Tag2>
                                                                        <Document>
                                                                          <Tag3>dataC</Tag3>
                                                                          <Tag4>777</Tag4>
                                                                        </Document>
                                                                      </Item>
                                                                    </Root>
                                                                    """;
}
