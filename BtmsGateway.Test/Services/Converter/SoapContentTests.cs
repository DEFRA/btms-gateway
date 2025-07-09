using BtmsGateway.Exceptions;
using BtmsGateway.Services.Converter;
using FluentAssertions;

namespace BtmsGateway.Test.Services.Converter;

public class SoapContentTests
{
    private const string Declaration = "<?xml version=\"1.0\" encoding=\"utf-8\"?>";

    private static readonly string s_testDataPath = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory,
        "Services",
        "Converter",
        "Fixtures"
    );

    [Fact]
    public void When_retrieving_message_at_single_element_xpath_against_soap_without_namespaces_Then_should_get_message()
    {
        const string soap = $"{Declaration}<Envelope><Body><Message1><Data>111</Data></Message1></Body></Envelope>";
        var soapContent = new SoapContent(soap);

        soapContent.GetMessage("Message1").Should().Be("<Message1><Data>111</Data></Message1>");
    }

    [Fact]
    public void When_retrieving_message_at_single_element_xpath_against_soap_with_namespaces_Then_should_get_message()
    {
        const string soap =
            $"{Declaration}<s:Envelope xmlns:s=\"http://www.w3.org/2003/05/soap-envelope\"><s:Body><m:Message1 xmlns:m=\"http://local1\"><Data xmlns=\"http://local2\">111</Data></m:Message1></s:Body></s:Envelope>";
        var soapContent = new SoapContent(soap);

        soapContent
            .GetMessage("Message1")
            .Should()
            .Be("<m:Message1 xmlns:m=\"http://local1\"><Data xmlns=\"http://local2\">111</Data></m:Message1>");
    }

    [Fact]
    public void When_retrieving_message_at_multi_element_xpath_against_soap_without_namespaces_Then_should_get_message()
    {
        const string soap =
            $"{Declaration}<Envelope><Body><Message1><Message2><Data>111</Data></Message2></Message1></Body></Envelope>";
        var soapContent = new SoapContent(soap);

        soapContent.GetMessage("Message1/Message2").Should().Be("<Message2><Data>111</Data></Message2>");
    }

    [Fact]
    public void When_retrieving_message_at_multi_element_xpath_against_soap_with_namespaces_Then_should_get_message()
    {
        const string soap =
            $"{Declaration}<s:Envelope xmlns:s=\"http://www.w3.org/2003/05/soap-envelope\"><s:Body><m:Message1 xmlns:m=\"http://local1\"><n:Message2 xmlns:n=\"http://local3\"><Data xmlns=\"http://local2\">111</Data></n:Message2></m:Message1></s:Body></s:Envelope>";
        var soapContent = new SoapContent(soap);

        soapContent
            .GetMessage("Message1/Message2")
            .Should()
            .Be("<n:Message2 xmlns:n=\"http://local3\"><Data xmlns=\"http://local2\">111</Data></n:Message2>");
    }

    [Fact]
    public void When_checking_single_element_xpath_against_soap_without_namespaces_Then_should_find_message()
    {
        const string soap = $"{Declaration}<Envelope><Body><Message1><Data>111</Data></Message1></Body></Envelope>";
        var soapContent = new SoapContent(soap);

        soapContent.HasMessage("Message1").Should().BeTrue();
    }

    [Fact]
    public void When_checking_single_element_xpath_against_soap_with_namespaces_Then_should_find_message()
    {
        const string soap =
            $"{Declaration}<s:Envelope xmlns:s=\"http://www.w3.org/2003/05/soap-envelope\"><s:Body><m:Message1 xmlns:m=\"http://local1\"><Data xmlns=\"http://local2\">111</Data></m:Message1></s:Body></s:Envelope>";
        var soapContent = new SoapContent(soap);

        soapContent.HasMessage("Message1").Should().BeTrue();
    }

    [Fact]
    public void When_checking_multi_element_xpath_against_soap_without_namespaces_Then_should_find_message()
    {
        const string soap =
            $"{Declaration}<Envelope><Body><Message1><Message2><Data>111</Data></Message2></Message1></Body></Envelope>";
        var soapContent = new SoapContent(soap);

        soapContent.HasMessage("Message1/Message2").Should().BeTrue();
    }

    [Fact]
    public void When_checking_multi_element_xpath_against_soap_with_namespaces_Then_should_find_message()
    {
        const string soap =
            $"{Declaration}<s:Envelope xmlns:s=\"http://www.w3.org/2003/05/soap-envelope\"><s:Body><m:Message1 xmlns:m=\"http://local1\"><n:Message2 xmlns:n=\"http://local3\"><Data xmlns=\"http://local2\">111</Data></n:Message2></m:Message1></s:Body></s:Envelope>";
        var soapContent = new SoapContent(soap);

        soapContent.HasMessage("Message1/Message2").Should().BeTrue();
    }

    [Fact]
    public void When_retrieving_property_at_single_element_xpath_against_soap_without_namespaces_Then_should_get_property()
    {
        const string soap = $"{Declaration}<Envelope><Body><Message1><Data>111</Data></Message1></Body></Envelope>";
        var soapContent = new SoapContent(soap);

        soapContent.GetProperty("Data").Should().Be("111");
    }

    [Fact]
    public void When_retrieving_property_at_single_element_xpath_against_soap_with_namespaces_Then_should_get_property()
    {
        const string soap =
            $"{Declaration}<s:Envelope xmlns:s=\"http://www.w3.org/2003/05/soap-envelope\"><s:Body><m:Message1 xmlns:m=\"http://local1\"><Data xmlns=\"http://local2\">111</Data></m:Message1></s:Body></s:Envelope>";
        var soapContent = new SoapContent(soap);

        soapContent.GetProperty("Data").Should().Be("111");
    }

    [Fact]
    public void When_retrieving_property_at_multi_element_xpath_against_soap_without_namespaces_Then_should_get_property()
    {
        const string soap = $"{Declaration}<Envelope><Body><Message1><Data>111</Data></Message1></Body></Envelope>";
        var soapContent = new SoapContent(soap);

        soapContent.GetProperty("Message1/Data").Should().Be("111");
    }

    [Fact]
    public void When_retrieving_property_at_multi_element_xpath_against_soap_with_namespaces_Then_should_get_property()
    {
        const string soap =
            $"{Declaration}<s:Envelope xmlns:s=\"http://www.w3.org/2003/05/soap-envelope\"><s:Body><m:Message1 xmlns:m=\"http://local1\"><Data xmlns=\"http://local2\">111</Data></m:Message1></s:Body></s:Envelope>";
        var soapContent = new SoapContent(soap);

        soapContent.GetProperty("Message1/Data").Should().Be("111");
    }

    [Fact]
    public void When_soap_value_contains_xml_character_entities_Then_the_soap_string_should_successfully_parse_into_soap_content()
    {
        var soap = File.ReadAllText(Path.Combine(s_testDataPath, "ClearanceRequestWithXmlCharacterEntityValues.xml"));

        var soapContent = new SoapContent(soap);

        soapContent
            .SoapString.Should()
            .Contain("<GoodsDescription>XML Character Entities &quot; &apos; &lt; &gt; &amp;</GoodsDescription>");
    }

    [Fact]
    public void When_soap_is_unparsable_Then_invalid_soap_exception_should_be_thrown()
    {
        const string soap = $"{Declaration}<Envelope><Body><Message1><Data>&</Data></Message1></Body></Envelope>";

        var thrownException = Assert.Throws<InvalidSoapException>(() => new SoapContent(soap));
        thrownException.Message.Should().Be("Invalid SOAP Message");
        thrownException.InnerException.Should().NotBeNull();
    }
}
