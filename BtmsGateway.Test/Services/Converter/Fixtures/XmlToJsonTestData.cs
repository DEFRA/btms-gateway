using BtmsGateway.Test.TestUtils;

namespace BtmsGateway.Test.Services.Converter.Fixtures;

public class XmlToJsonTestData : TheoryData<string, string, string>
{
    public XmlToJsonTestData()
    {
      Add("Simple self-closing tag", XmlSimpleSelfClosing, JsonSimpleNull.LinuxLineEndings());
      Add("Simple self-closing tag w/ space", XmlSimpleSelfClosingWithSpace, JsonSimpleNull.LinuxLineEndings());
      Add("Simple empty tag", XmlSimpleEmpty, JsonSimpleEmpty.LinuxLineEndings());
      Add("Simple content tag", XmlSimpleContent, JsonSimpleContent.LinuxLineEndings());
      Add("Complex single level", XmlComplexSingleLevel, JsonComplexSingleLevel.LinuxLineEndings());
      Add("Complex multi level", XmlComplexMultiLevel, JsonComplexMultiLevel.LinuxLineEndings());
      Add("Complex multi level w/ Items", XmlComplexMultiLevelWithItems, JsonComplexMultiLevelWithItems.LinuxLineEndings());
      Add("Complex multi level w/ single item Items", XmlComplexMultiLevelWithSingleItemItems, JsonComplexMultiLevelWithSingleItemItems.LinuxLineEndings());
      Add("Complex multi level SOAP", XmlComplexMultiLevelWithNamespace, JsonComplexMultiLevel.LinuxLineEndings());
    }

    private const string XmlSimpleSelfClosing = "<Root/>";
    private const string XmlSimpleSelfClosingWithSpace = "<Root />";
    private const string XmlSimpleEmpty = "<Root></Root>";
    private const string XmlSimpleContent = "<Root>data</Root>";

    private const string XmlComplexSingleLevel = """
                                                 <Root>
                                                   <Tag1>data1</Tag1>
                                                   <Tag2>data2</Tag2>
                                                 </Root>
                                                 """;

    private const string XmlComplexMultiLevel = """
                                                <?xml version="1.0" encoding="UTF-8"?>

                                                <Root>
                                                  <Element1>
                                                    <Tag1>data1</Tag1>
                                                    <Tag2>12.3</Tag2>
                                                  </Element1>
                                                  
                                                  <Element2>
                                                    <Tag3>true</Tag3>
                                                    <Element3>
                                                      <Tag4/>
                                                      
                                                      <Tag5></Tag5>
                                                      <Tag6>123</Tag6>
                                                      <Tag7>abc
                                                def
                                                ghi</Tag7>
                                                    </Element3>
                                                  </Element2>
                                                </Root>
                                                """;

    private const string XmlComplexMultiLevelWithItems = """
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
                                                              <AnotherDocument>
                                                                <Tag1>dataD</Tag1>
                                                                <Tag2>dataE</Tag2>
                                                              </AnotherDocument>
                                                              <Document>
                                                                <Tag3>dataD</Tag3>
                                                                <Tag4>888</Tag4>
                                                              </Document>
                                                            </Item>
                                                          </Root>
                                                          """;

    private const string XmlComplexMultiLevelWithSingleItemItems = """
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

    private const string XmlComplexMultiLevelWithNamespace = """
                                                                 <?xml version="1.0" encoding="UTF-8"?>

                                                                 <Root xmlns="http://www.w3.org/2003/05/soap-envelope/" xmlns:i="http://www.w3.org/2001/XMLSchema-instance">
                                                                   <i:Element1 xmlns:x="http://localtypes/">
                                                                     <x:Tag1>data1</x:Tag1>
                                                                     <x:Tag2>12.3</x:Tag2>
                                                                   </i:Element1>
                                                                   <i:Element2 xmlns:x="http://localtypes/">
                                                                     <x:Tag3>true</x:Tag3>
                                                                     <i:Element3>
                                                                       <x:Tag4/>
                                                                       <x:Tag5></x:Tag5>
                                                                       <x:Tag6>123</x:Tag6>
                                                                       <x:Tag7>abc
                                                                 def
                                                                 ghi</x:Tag7>
                                                                     </i:Element3>
                                                                   </i:Element2>
                                                                 </Root>
                                                                 """;

    private const string JsonSimpleNull = """
                                          {
                                            "root": null
                                          }
                                          """;

    private const string JsonSimpleEmpty = """
                                           {
                                             "root": ""
                                           }
                                           """;

    private const string JsonSimpleContent = """
                                             {
                                               "root": "data"
                                             }
                                             """;

    private const string JsonComplexSingleLevel = """
                                                  {
                                                    "root": {
                                                      "tag1": "data1",
                                                      "tag2": "data2"
                                                    }
                                                  }
                                                  """;

    private const string JsonComplexMultiLevel = """
                                                 {
                                                   "root": {
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
                                                 }
                                                 """;

    private const string JsonComplexMultiLevelWithItems = """
                                                           {
                                                             "root": {
                                                               "Items": [
                                                                 {
                                                                   "tag1": "dataA",
                                                                   "tag2": 123
                                                                 },
                                                                 {
                                                                   "tag1": "dataB",
                                                                   "tag2": 456,
                                                                   "Documents": [
                                                                     {
                                                                       "tag3": "dataC",
                                                                       "tag4": "777"
                                                                     },
                                                                     {
                                                                       "tag3": "dataD",
                                                                       "tag4": "888"
                                                                     }
                                                                   ],
                                                                   "anotherDocuments": [
                                                                     {
                                                                       "tag1": "dataD",
                                                                       "tag2": "dataE"
                                                                     }
                                                                   ]
                                                                 }
                                                               ]
                                                             }
                                                           }
                                                           """;

    private const string JsonComplexMultiLevelWithSingleItemItems = """
                                                                     {
                                                                       "root": {
                                                                         "Items": [
                                                                           {
                                                                             "tag1": "dataB",
                                                                             "tag2": 456,
                                                                             "Documents": [
                                                                               {
                                                                                 "tag3": "dataC",
                                                                                 "tag4": "777"
                                                                               }
                                                                             ]
                                                                           }
                                                                         ]
                                                                       }
                                                                     }
                                                                     """;
}