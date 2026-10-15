using System.Text.Json.Serialization;

namespace BtmsGateway.Contract.CustomsDeclarations;

public record InboundError
{
    [JsonPropertyName("header")]
    public required InboundErrorHeader Header { get; init; }

    [JsonPropertyName("serviceHeader")]
    public required ServiceHeader ServiceHeader { get; init; }

    [JsonPropertyName("errors")]
    public required InboundErrorItem[] Errors { get; init; }
}
