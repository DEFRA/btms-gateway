using System.Text.Json.Serialization;

namespace BtmsGateway.Contract.CustomsDeclarations;

public record FinalisationHeader : Header
{
    [JsonPropertyName("decisionNumber")]
    public int? DecisionNumber { get; set; }

    [JsonPropertyName("finalState")]
    public required string FinalState { get; init; }

    [JsonPropertyName("manualAction")]
    public required string ManualAction { get; init; }
}
