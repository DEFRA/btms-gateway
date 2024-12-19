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
  
    private const string XmlComplexMultiLevelWithArrays = """
                                                          <Root>
                                                            <Array>
                                                              <Tag1>dataA</Tag1>
                                                              <Tag2>123</Tag2>
                                                            </Array>
                                                            <Array>
                                                              <Tag1>dataB</Tag1>
                                                              <Tag2>456</Tag2>
                                                              <List>
                                                                <Tag3>dataC</Tag3>
                                                                <Tag4>777</Tag4>
                                                              </List>
                                                              <List>
                                                                <Tag3>dataD</Tag3>
                                                                <Tag4>888</Tag4>
                                                              </List>
                                                            </Array>
                                                          </Root>
                                                          """;
  
    private const string XmlComplexMultiLevelWithSingleItemArrays = """
                                                                    <Root>
                                                                      <Array>
                                                                        <Tag1>dataB</Tag1>
                                                                        <Tag2>456</Tag2>
                                                                        <List>
                                                                          <Tag3>dataC</Tag3>
                                                                          <Tag4>777</Tag4>
                                                                        </List>
                                                                      </Array>
                                                                    </Root>
                                                                    """;
  
    private const string XmlComplexMultiLevelSoap = """
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