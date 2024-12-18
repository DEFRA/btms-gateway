namespace BtmsGateway.Test.Services.Converter;

public class XmlToJsonTestData : TheoryData<string, string, string>
{
    public XmlToJsonTestData()
    {
        Add("Simple self-closing tag", XmlSimpleSelfClosing, JsonSimpleNull);
        Add("Simple self-closing tag w/ space", XmlSimpleSelfClosingWithSpace, JsonSimpleNull);
        Add("Simple empty tag", XmlSimpleEmpty, JsonSimpleEmpty);
        Add("Simple content tag", XmlSimpleContent, JsonSimpleContent);
        Add("Complex single level", XmlComplexSingleLevel, JsonComplexSingleLevel);
        Add("Complex multi level", XmlComplexMultiLevel, JsonComplexMultiLevel);
        Add("Complex multi level w/ arrays", XmlComplexMultiLevelWithArrays, JsonComplexMultiLevelWithArrays);
        Add("Complex multi level w/ single item arrays", XmlComplexMultiLevelWithSingleItemArrays, JsonComplexMultiLevelWithSingleItemArrays);
        Add("Complex multi level SOAP", XmlComplexMultiLevelSoap, JsonComplexMultiLevel);
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
                                                <?xml version="1.0" encoding="UTF-8"?>

                                                <root>
                                                  <element1>
                                                    <tag1>data1</tag1>
                                                    <tag2>12.3</tag2>
                                                  </element1>
                                                  
                                                  <element2>
                                                    <tag3>true</tag3>
                                                    <element3>
                                                      <tag4/>
                                                      
                                                      <tag5></tag5>
                                                      <tag6>123</tag6>
                                                      <tag7>abc
                                                def
                                                ghi</tag7>
                                                    </element3>
                                                  </element2>
                                                </root>
                                                """;
  
    private const string XmlComplexMultiLevelWithArrays = """
                                                          <root>
                                                            <array>
                                                              <tag1>dataA</tag1>
                                                              <tag2>123</tag2>
                                                            </array>
                                                            <array>
                                                              <tag1>dataB</tag1>
                                                              <tag2>456</tag2>
                                                              <list>
                                                                <tag3>dataC</tag3>
                                                                <tag4>777</tag4>
                                                              </list>
                                                              <list>
                                                                <tag3>dataD</tag3>
                                                                <tag4>888</tag4>
                                                              </list>
                                                            </array>
                                                          </root>
                                                          """;
  
    private const string XmlComplexMultiLevelWithSingleItemArrays = """
                                                                    <root>
                                                                      <array>
                                                                        <tag1>dataB</tag1>
                                                                        <tag2>456</tag2>
                                                                        <list>
                                                                          <tag3>dataC</tag3>
                                                                          <tag4>777</tag4>
                                                                        </list>
                                                                      </array>
                                                                    </root>
                                                                    """;
  
    private const string XmlComplexMultiLevelSoap = """
                                                    <?xml version="1.0" encoding="UTF-8"?>

                                                    <root xmlns="http://www.w3.org/2003/05/soap-envelope/" xmlns:i="http://www.w3.org/2001/XMLSchema-instance">
                                                      <i:element1 xmlns:x="http://localtypes/">
                                                        <x:tag1>data1</x:tag1>
                                                        <x:tag2>12.3</x:tag2>
                                                      </i:element1>
                                                      <i:element2 xmlns:x="http://localtypes/">
                                                        <x:tag3>true</x:tag3>
                                                        <i:element3>
                                                          <x:tag4/>
                                                          <x:tag5></x:tag5>
                                                          <x:tag6>123</x:tag6>
                                                          <x:tag7>abc
                                                    def
                                                    ghi</x:tag7>
                                                        </i:element3>
                                                      </i:element2>
                                                    </root>
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
  
    private const string JsonComplexMultiLevelWithArrays = """
                                                           {
                                                             "root": {
                                                               "arrays": [
                                                                 {
                                                                   "tag1": "dataA",
                                                                   "tag2": 123
                                                                 },
                                                                 {
                                                                   "tag1": "dataB",
                                                                   "tag2": 456,
                                                                   "lists": [
                                                                     {
                                                                       "tag3": "dataC",
                                                                       "tag4": 777
                                                                     },
                                                                     {
                                                                       "tag3": "dataD",
                                                                       "tag4": 888
                                                                     }
                                                                   ]
                                                                 }
                                                               ]
                                                             }
                                                           }
                                                           """;
  
    private const string JsonComplexMultiLevelWithSingleItemArrays = """
                                                                     {
                                                                       "root": {
                                                                         "arrays": [
                                                                           {
                                                                             "tag1": "dataB",
                                                                             "tag2": 456,
                                                                             "lists": [
                                                                               {
                                                                                 "tag3": "dataC",
                                                                                 "tag4": 777
                                                                               }
                                                                             ]
                                                                           }
                                                                         ]
                                                                       }
                                                                     }
                                                                     """;
}