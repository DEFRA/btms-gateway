using System.Text.Json.Serialization;

namespace BtmsGateway.Contract.CustomsDeclarations;

public record CustomsDeclarationsBase
{
    [JsonPropertyName("serviceHeader")]
    public required ServiceHeader ServiceHeader { get; init; }
}
