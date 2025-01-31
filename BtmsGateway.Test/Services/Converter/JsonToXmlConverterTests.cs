using BtmsGateway.Services.Converter;
using BtmsGateway.Test.Services.Converter.Fixtures;
using BtmsGateway.Test.TestUtils;
using FluentAssertions;

namespace BtmsGateway.Test.Services.Converter;

public class JsonToXmlConverterTests
{
    private static readonly string TestDataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Services", "Converter", "Fixtures");
    
    [Theory]
    [ClassData(typeof(JsonToXmlTestData))]
    public void When_receiving_valid_json_Then_should_convert_to_xml(string because, string json, string rootName, string expectedXml)
    {
        KnownArray[] knownArrays = [ new() { ItemName = "Array", ArrayName = "Arrays" }, new() { ItemName = "List", ArrayName = "Lists" }, new() { ItemName = "AnotherList", ArrayName = "AnotherLists" } ];
        
        JsonToXmlConverter.Convert(json, knownArrays, rootName).LinuxLineEndings().Should().Be(expectedXml, because);
    }

    [Fact]
    public void When_receiving_clearance_request_json_Then_should_convert_to_xml()
    {
        var json = File.ReadAllText(Path.Combine(TestDataPath, "ClearanceRequest.json"));
        var expectedXml = File.ReadAllText(Path.Combine(TestDataPath, "ClearanceRequestNotSoap.xml")).LinuxLineEndings();
        KnownArray[] knownArrays = [ new() { ItemName = "Item", ArrayName = "Items" }, new() { ItemName = "Document", ArrayName = "Documents" }, new() { ItemName = "Check", ArrayName = "Checks" } ];
        
        JsonToXmlConverter.Convert(json, knownArrays, "ALVSClearanceRequest").LinuxLineEndings().Should().Be(expectedXml);
    }

    [Fact]
    public void When_receiving_invalid_json_Then_should_fail()
    {
        var act = () => JsonToXmlConverter.Convert("{\"abc\"", [], "Root");
        
        act.Should().Throw<ArgumentException>();
    }
}