using System.Text.Json.Serialization;

namespace BtmsGateway.Contract.CustomsDeclarations;

public record Header
{
    [JsonPropertyName("entryReference")]
    public string? EntryReference { get; set; }

    [JsonPropertyName("entryVersionNumber")]
    public int? EntryVersionNumber { get; set; }
}
