using BtmsGateway.Services.Converter;
using BtmsGateway.Test.TestUtils;
using FluentAssertions;

namespace BtmsGateway.Test.Services.Converter;

public class SoapToJsonConverterTests
{
    private static readonly string TestDataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Services", "Converter", "Fixtures");
    
    [Theory]
    [InlineData("ClearanceRequestSoap.xml", 1, "ClearanceRequest.json")]
    [InlineData("ClearanceRequestSoapForIpaffs.xml", 2, "ClearanceRequest.json")]
    [InlineData("AlvsToCdsDecisionNotification.xml", 2, "DecisionNotification.json")]
    public void When_receiving_clearance_request_soap_Then_should_convert_to_json(string soapFileName, int messageBodyDepth, string jsonFileName)
    {
        var soap = File.ReadAllText(Path.Combine(TestDataPath, soapFileName));
        var json = File.ReadAllText(Path.Combine(TestDataPath, jsonFileName)).LinuxLineEndings();

        SoapToJsonConverter.Convert(soap, messageBodyDepth).LinuxLineEndings().Should().Be(json);
    }
}