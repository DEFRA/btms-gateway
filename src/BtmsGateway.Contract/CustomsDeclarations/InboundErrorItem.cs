using System.Text.Json.Serialization;

namespace BtmsGateway.Contract.CustomsDeclarations;

public record InboundErrorItem
{
    [JsonPropertyName("errorCode")]
    public required string ErrorCode { get; init; }

    [JsonPropertyName("errorMessage")]
    public required string ErrorMessage { get; init; }
}
