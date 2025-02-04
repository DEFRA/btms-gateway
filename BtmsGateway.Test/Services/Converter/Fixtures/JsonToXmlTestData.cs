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
}