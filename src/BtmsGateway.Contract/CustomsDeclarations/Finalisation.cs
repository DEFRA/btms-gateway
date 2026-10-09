using System.Text.Json.Serialization;

namespace BtmsGateway.Contract.CustomsDeclarations;

public record Finalisation
{
    [JsonPropertyName("header")]
    public required FinalisationHeader Header { get; init; }

    [JsonPropertyName("serviceHeader")]
    public required ServiceHeader ServiceHeader { get; init; }
}
