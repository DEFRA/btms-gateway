using System.Text.Json.Serialization;

namespace BtmsGateway.Contract.CustomsDeclarations;

public record Header
{
    [JsonPropertyName("entryReference")]
    public string? EntryReference { get; init; }

    [JsonPropertyName("entryVersionNumber")]
    public int? EntryVersionNumber { get; init; }
}
