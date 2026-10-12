using System.Text.Json.Serialization;
using BtmsGateway.Contract.Json;

namespace BtmsGateway.Contract.CustomsDeclarations;

public record ServiceHeader
{
    [JsonPropertyName("sourceSystem")]
    public required string SourceSystem { get; init; }

    [JsonPropertyName("destinationSystem")]
    public required string DestinationSystem { get; init; }

    [JsonPropertyName("correlationId")]
    public string? CorrelationId { get; set; }

    [JsonPropertyName("serviceCallTimestamp")]
    [EpochDateTimeJsonConverter]
    public DateTime? ServiceCallTimestamp { get; set; }
}
