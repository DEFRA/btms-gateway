using System.Text.Json;
using System.Xml.Serialization;
using BtmsGateway.Contract.CustomsDeclarations;
using FluentAssertions;

namespace BtmsGateway.Test.Contract.CustomsDeclarations;

public class ClearanceRequestHeaderXmlTests
{
    [Fact]
    public void When_deserializing_xml_compact_arrival_datetime_Then_it_should_populate_arrival_datetime()
    {
        const string xml = """
            <ClearanceRequestHeader>
              <ArrivalDateTime>202201031224</ArrivalDateTime>
            </ClearanceRequestHeader>
            """;

        var serializer = new XmlSerializer(
            typeof(ClearanceRequestHeader),
            new XmlRootAttribute("ClearanceRequestHeader")
        );

        using var reader = new StringReader(xml);
        var header = (ClearanceRequestHeader)serializer.Deserialize(reader)!;

        header.ArrivalDateTime.Should().Be(new DateTime(2022, 1, 3, 12, 24, 0));
    }

    [Fact]
    public void When_serializing_xml_with_arrival_datetime_Then_it_should_use_compact_arrival_datetime()
    {
        var header = new ClearanceRequestHeader { ArrivalDateTime = new DateTime(2022, 1, 3, 12, 24, 0) };
        var serializer = new XmlSerializer(
            typeof(ClearanceRequestHeader),
            new XmlRootAttribute("ClearanceRequestHeader")
        );

        using var writer = new StringWriter();
        serializer.Serialize(writer, header);

        writer.ToString().Should().Contain("<ArrivalDateTime>202201031224</ArrivalDateTime>");
    }

    [Fact]
    public void When_serializing_to_json_Then_xml_proxy_property_should_not_be_included()
    {
        var header = new ClearanceRequestHeader { ArrivalDateTime = new DateTime(2022, 1, 3, 12, 24, 0) };

        var json = JsonSerializer.Serialize(header);

        using var document = JsonDocument.Parse(json);
        document.RootElement.TryGetProperty("arrivalDateTime", out _).Should().BeTrue();
        document.RootElement.TryGetProperty("arrivalDateTimeXml", out _).Should().BeFalse();
    }
}
