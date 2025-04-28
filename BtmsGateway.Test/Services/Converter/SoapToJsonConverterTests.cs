using BtmsGateway.Services.Converter;
using BtmsGateway.Test.TestUtils;
using FluentAssertions;

namespace BtmsGateway.Test.Services.Converter;

public class SoapToJsonConverterTests
{
    private static readonly string TestDataPath = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory,
        "Services",
        "Converter",
        "Fixtures"
    );

    [Theory]
    [InlineData("ClearanceRequestSoap.xml", "ALVSClearanceRequest", "ClearanceRequest.json")]
    [InlineData(
        "ClearanceRequestSoapForIpaffs.xml",
        "ALVSClearanceRequestPost/ALVSClearanceRequest",
        "ClearanceRequest.json"
    )]
    [InlineData(
        "AlvsToCdsDecisionNotification.xml",
        "DecisionNotification/DecisionNotification",
        "DecisionNotification.json"
    )]
    [InlineData("AlvsErrorNotification.xml", "ALVSErrorNotificationRequest", "AlvsErrorNotification.json")]
    [InlineData(
        "HmrcErrorNotification.xml",
        "HMRCErrorNotification/HMRCErrorNotification",
        "HmrcErrorNotification.json"
    )]
    [InlineData(
        "HmrcErrorNotificationWithHtmlEncoding.xml",
        "HMRCErrorNotification/HMRCErrorNotification",
        "HmrcErrorNotification.json"
    )]
    public void When_receiving_clearance_request_soap_Then_should_convert_to_json(
        string soapFileName,
        string messageSubXPath,
        string jsonFileName
    )
    {
        var soapContent = new SoapContent(File.ReadAllText(Path.Combine(TestDataPath, soapFileName)));
        var json = File.ReadAllText(Path.Combine(TestDataPath, jsonFileName)).LinuxLineEndings();

        SoapToJsonConverter.Convert(soapContent, messageSubXPath).LinuxLineEndings().Should().Be(json);
    }

    [Fact]
    public void When_soap_content_does_not_contain_message_type_sub_path_Then_should_throw_exception()
    {
        var soapContent = new SoapContent(File.ReadAllText(Path.Combine(TestDataPath, "ClearanceRequestSoap.xml")));

        var thrownException = Assert.Throws<ArgumentException>(() =>
            SoapToJsonConverter.Convert(soapContent, "NonExistingMessageSubPath")
        );
        thrownException.Message.Should().Be("The XML message is not valid");
        thrownException.InnerException.Should().BeOfType<InvalidDataException>();
        thrownException.InnerException?.Message.Should().Be("The SOAP XML does not contain a message");
    }
}
