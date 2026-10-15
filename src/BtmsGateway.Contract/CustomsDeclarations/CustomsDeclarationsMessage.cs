using System.Text.Json.Serialization;

namespace BtmsGateway.Contract.CustomsDeclarations;

public record CustomsDeclarationsMessage : CustomsDeclarationsBase
{
    [JsonPropertyName("header")]
    public required Header Header { get; init; }
}
