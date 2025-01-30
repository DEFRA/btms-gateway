using BtmsGateway.Services.Converter;
using BtmsGateway.Test.TestUtils;
using FluentAssertions;

namespace BtmsGateway.Test.Services.Converter;

public class SoapToJsonConverterTests
{
    private static readonly string TestDataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Services", "Converter", "Fixtures");
    
    [Theory]
    [InlineData("ClearanceRequestSoap.xml", 1)]
    [InlineData("ClearanceRequestSoapWithMessageLevels.xml", 2)]
    public void When_receiving_clearance_request_soap_Then_should_convert_to_json(string soapFileName, int messageInBodyDepth)
    {
        var xml = File.ReadAllText(Path.Combine(TestDataPath, soapFileName));
        var json = File.ReadAllText(Path.Combine(TestDataPath, "ClearanceRequest.json")).LinuxLineEndings();
        var knownArrays = new Dictionary<string, string> { { "Item", "Items" }, { "Document", "Documents" }, { "Check", "Checks" } };
        
        SoapToJsonConverter.Convert(xml, knownArrays, messageInBodyDepth).LinuxLineEndings().Should().Be(json);
    }
}