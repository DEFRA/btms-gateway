using BtmsGateway.Services.Converter;
using BtmsGateway.Test.TestUtils;
using FluentAssertions;

namespace BtmsGateway.Test.Services.Converter;

public class SoapToJsonConverterTests
{
    private static readonly string TestDataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Services", "Converter", "Fixtures");

    [Theory]
    [InlineData("ClearanceRequestSoap.xml", "ALVSClearanceRequest", "ClearanceRequest.json")]
    [InlineData("ClearanceRequestSoapForIpaffs.xml", "ALVSClearanceRequestPost/ALVSClearanceRequest", "ClearanceRequest.json")]
    [InlineData("AlvsToCdsDecisionNotification.xml", "DecisionNotification/DecisionNotification", "DecisionNotification.json")]
    [InlineData("AlvsErrorNotification.xml", "ALVSErrorNotificationRequest", "AlvsErrorNotification.json")]
    [InlineData("HmrcErrorNotification.xml", "HMRCErrorNotification/HMRCErrorNotification", "HmrcErrorNotification.json")]
    [InlineData("HmrcErrorNotificationWithHtmlEncoding.xml", "HMRCErrorNotification/HMRCErrorNotification", "HmrcErrorNotification.json")]
    public void When_receiving_clearance_request_soap_Then_should_convert_to_json(string soapFileName, string messageSubXPath, string jsonFileName)
    {
        var soapContent = new SoapContent(File.ReadAllText(Path.Combine(TestDataPath, soapFileName)));
        var json = File.ReadAllText(Path.Combine(TestDataPath, jsonFileName)).LinuxLineEndings();

        SoapToJsonConverter.Convert(soapContent, messageSubXPath).LinuxLineEndings().Should().Be(json);
    }
}