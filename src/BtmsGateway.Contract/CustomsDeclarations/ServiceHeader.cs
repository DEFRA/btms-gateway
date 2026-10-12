using System.Text.Json.Serialization;
using System.Xml.Serialization;
using BtmsGateway.Contract.Json;
using BtmsGateway.Contract.Xml;

namespace BtmsGateway.Contract.CustomsDeclarations;

public record ServiceHeader
{
    [JsonPropertyName("sourceSystem")]
    public required string SourceSystem { get; init; }

    [JsonPropertyName("destinationSystem")]
    public required string DestinationSystem { get; init; }

    [JsonPropertyName("correlationId")]
    public string? CorrelationId { get; set; }

    [XmlIgnore]
    [JsonPropertyName("serviceCallTimestamp")]
    [EpochDateTimeJsonConverter]
    public DateTime? ServiceCallTimestamp { get; set; }

    [XmlElement("ServiceCallTimestamp")]
    [JsonIgnore]
    public string? ServiceCallTimestampXml
    {
        get => DateTimeUtilities.ToCompactXmlDateTime(ServiceCallTimestamp);
        set => ServiceCallTimestamp = DateTimeUtilities.ParseNullableXmlDateTime(value);
    }
}
