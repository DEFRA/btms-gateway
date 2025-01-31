using BtmsGateway.Services.Converter;
using BtmsGateway.Test.Services.Converter.Fixtures;
using BtmsGateway.Test.TestUtils;
using FluentAssertions;

namespace BtmsGateway.Test.Services.Converter;

public class XmlToJsonConverterTests
{
    private static readonly string TestDataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Services", "Converter", "Fixtures");

    [Theory]
    [ClassData(typeof(XmlToJsonTestData))]
    public void When_receiving_valid_xml_Then_should_convert_to_json(string because, string xml, string expectedJson)
    {
        KnownArray[] knownArrays = [ new() { ItemName = "Array", ArrayName = "Arrays" }, new() { ItemName = "List", ArrayName = "Lists" }, new() { ItemName = "AnotherList", ArrayName = "AnotherLists" } ];
        
        XmlToJsonConverter.Convert(xml, knownArrays).LinuxLineEndings().Should().Be(expectedJson, because);
    }

    [Fact]
    public void When_receiving_clearance_request_not_soap_Then_should_convert_to_json()
    {
        var xml = File.ReadAllText(Path.Combine(TestDataPath, "ClearanceRequestNotSoap.xml"));
        var expectedJson = File.ReadAllText(Path.Combine(TestDataPath, "ClearanceRequestWithRoot.json")).LinuxLineEndings();
        KnownArray[] knownArrays = [ new() { ItemName = "Item", ArrayName = "Items" }, new() { ItemName = "Document", ArrayName = "Documents" }, new() { ItemName = "Check", ArrayName = "Checks" } ];
        
        XmlToJsonConverter.Convert(xml, knownArrays).LinuxLineEndings().Should().Be(expectedJson);
    }

    [Fact]
    public void When_receiving_invalid_xml_Then_should_fail()
    {
        var act = () => XmlToJsonConverter.Convert("<root><not-root>", []);
        
        act.Should().Throw<ArgumentException>();
    }
}