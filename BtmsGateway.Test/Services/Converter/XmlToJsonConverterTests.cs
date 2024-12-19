using BtmsGateway.Services.Converter;
using FluentAssertions;

namespace BtmsGateway.Test.Services.Converter;

public class XmlToJsonConverterTests
{
    private static readonly string TestDataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Services", "Converter");
    
    [Fact]
    public void When_receiving_clearance_request_soap_Then_should_convert_to_json()
    {
        var xml = File.ReadAllText(Path.Combine(TestDataPath, "ClearanceRequest.xml"));
        var json = File.ReadAllText(Path.Combine(TestDataPath, "ClearanceRequest.json"));
        var knownArrays = new Dictionary<string, string>() { { "Item", "Items" }, { "Document", "Documents" }, { "Check", "Checks" } };
        XmlToJsonConverter.Convert(xml, knownArrays).Should().Be(json);
    }

    [Theory]
    [ClassData(typeof(XmlToJsonTestData))]
    public void When_receiving_valid_xml_Then_should_convert_to_json(string because, string xml, string expectedJson)
    {
        var knownArrays = new Dictionary<string, string>() { { "Array", "Arrays" }, { "List", "Lists" } };
        XmlToJsonConverter.Convert(xml, knownArrays).Should().Be(expectedJson, because);
    }

    [Fact]
    public void When_receiving_invalid_xml_Then_should_fail()
    {
        var act = () => XmlToJsonConverter.Convert("<root><not-root>");
        act.Should().Throw<ArgumentException>();
    }
}