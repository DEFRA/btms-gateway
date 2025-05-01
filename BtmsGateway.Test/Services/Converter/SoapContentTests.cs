using BtmsGateway.Services.Converter;
using FluentAssertions;

namespace BtmsGateway.Test.Services.Converter;

public class SoapContentTests
{
    private const string Declaration = "<?xml version=\"1.0\" encoding=\"utf-8\"?>";

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
}
