using System.Text.Json.Serialization;

namespace BtmsGateway.Contract.CustomsDeclarations;

public record InboundErrorHeader : Header
{
    [JsonPropertyName("sourceCorrelationId")]
    public required string SourceCorrelationId { get; init; }
}
