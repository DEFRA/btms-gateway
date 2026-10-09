using System.Text.Json.Serialization;

namespace BtmsGateway.Contract.CustomsDeclarations;

public record InboundErrorItem
{
    [JsonPropertyName("errorCode")]
    public required string errorCode { get; init; }

    [JsonPropertyName("errorMessage")]
    public required string errorMessage { get; init; }
}
