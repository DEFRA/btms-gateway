using BtmsGateway.Services.Converter;
using BtmsGateway.Test.Services.Converter.Fixtures;
using BtmsGateway.Test.TestUtils;
using FluentAssertions;

namespace BtmsGateway.Test.Services.Converter;

public class XmlToJsonConverterTests
{
    private static readonly string TestDataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Services", "Converter", "Fixtures");
    
    [Theory]
    [InlineData("ClearanceRequestWithEnvelope.xml")]
    [InlineData("ClearanceRequestWithoutEnvelope.xml")]
    public void When_receiving_clearance_request_soap_Then_should_convert_to_json(string soapFileName)
    {
        var xml = File.ReadAllText(Path.Combine(TestDataPath, soapFileName));
        var json = File.ReadAllText(Path.Combine(TestDataPath, "ClearanceRequest.json")).LinuxLineEndings();
        var knownArrays = new Dictionary<string, string> { { "Item", "Items" }, { "Document", "Documents" }, { "Check", "Checks" } };
        XmlToJsonConverter.Convert(xml, knownArrays).LinuxLineEndings().Should().Be(json);
    }

    [Theory]
    [ClassData(typeof(XmlToJsonTestData))]
    public void When_receiving_valid_xml_Then_should_convert_to_json(string because, string xml, string expectedJson)
    {
        var knownArrays = new Dictionary<string, string> { { "Array", "Arrays" }, { "List", "Lists" }, { "AnotherList", "AnotherLists" } };
        XmlToJsonConverter.Convert(xml, knownArrays).LinuxLineEndings().Should().Be(expectedJson, because);
    }

    [Fact]
    public void When_receiving_invalid_xml_Then_should_fail()
    {
        var act = () => XmlToJsonConverter.Convert("<root><not-root>", new Dictionary<string, string>());
        act.Should().Throw<ArgumentException>();
    }
}