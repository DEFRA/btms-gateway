using BtmsGateway.Services.Converter;
using BtmsGateway.Test.TestUtils;
using FluentAssertions;

namespace BtmsGateway.Test.Services.Converter;

public class JsonToSoapConverterTests
{
    private static readonly string TestDataPath = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory,
        "Services",
        "Converter",
        "Fixtures"
    );

    [Theory]
    [InlineData("ClearanceRequestSoap.xml", "ALVSClearanceRequest", SoapType.Cds, "ClearanceRequest.json")]
    [InlineData(
        "ClearanceRequestSoapForIpaffs.xml",
        "ALVSClearanceRequest",
        SoapType.AlvsToIpaffs,
        "ClearanceRequest.json"
    )]
    [InlineData(
        "AlvsToCdsDecisionNotification.xml",
        "DecisionNotification",
        SoapType.AlvsToCds,
        "DecisionNotification.json"
    )]
    public void When_receiving_clearance_request_soap_Then_should_convert_to_json(
        string soapFileName,
        string rootName,
        SoapType soapType,
        string jsonFileName
    )
    {
        var json = File.ReadAllText(Path.Combine(TestDataPath, jsonFileName));
        var soap = File.ReadAllText(Path.Combine(TestDataPath, soapFileName)).LinuxLineEndings();

        JsonToSoapConverter.Convert(json, rootName, soapType).LinuxLineEndings().Should().Be(soap);
    }
}
