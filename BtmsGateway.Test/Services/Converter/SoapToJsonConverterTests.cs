using BtmsGateway.Services.Converter;
using BtmsGateway.Test.TestUtils;
using FluentAssertions;

namespace BtmsGateway.Test.Services.Converter;

public class SoapToJsonConverterTests
{
    private static readonly string TestDataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Services", "Converter", "Fixtures");
    
    [Theory]
    [InlineData("ClearanceRequestSoap.xml", 1)]
    [InlineData("ClearanceRequestSoapForIpaffs.xml", 2)]
    public void When_receiving_clearance_request_soap_Then_should_convert_to_json(string soapFileName, int messageBodyDepth)
    {
        var soap = File.ReadAllText(Path.Combine(TestDataPath, soapFileName));
        var json = File.ReadAllText(Path.Combine(TestDataPath, "ClearanceRequest.json")).LinuxLineEndings();
        KnownArray[] knownArrays = [ new() { ItemName = "Item", ArrayName = "Items" }, new() { ItemName = "Document", ArrayName = "Documents" }, new() { ItemName = "Check", ArrayName = "Checks" } ];
        string[] knownNumbers = [ "EntryVersionNumber", "PreviousVersionNumber", "DecisionNumber", "ItemNumber", "ItemNetMass", "ItemSupplementaryUnits", "ItemThirdQuantity", "DocumentQuantity" ];

        SoapToJsonConverter.Convert(soap, knownArrays, knownNumbers, messageBodyDepth).LinuxLineEndings().Should().Be(json);
    }
}