using BtmsGateway.Services.Converter;
using BtmsGateway.Test.TestUtils;
using FluentAssertions;

namespace BtmsGateway.Test.Services.Converter;

public class JsonToSoapConverterTests
{
    private static readonly string TestDataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Services", "Converter", "Fixtures");
    
    [Theory]
    [InlineData("ClearanceRequestSoap.xml", true)]
    [InlineData("ClearanceRequestSoapForIpaffs.xml", false)]
    public void When_receiving_clearance_request_soap_Then_should_convert_to_json(string soapFileName, bool includeSoapHeader)
    {
        var json = File.ReadAllText(Path.Combine(TestDataPath, "ClearanceRequest.json")).LinuxLineEndings();
        var soap = File.ReadAllText(Path.Combine(TestDataPath, soapFileName));
        KnownArray[] knownArrays = [ new() { ItemName = "Item", ArrayName = "Items" }, new() { ItemName = "Document", ArrayName = "Documents" }, new() { ItemName = "Check", ArrayName = "Checks" } ];
        
        JsonToSoapConverter.Convert(json, knownArrays, "ALVSClearanceRequest", includeSoapHeader).LinuxLineEndings().Should().Be(soap);
    }
}