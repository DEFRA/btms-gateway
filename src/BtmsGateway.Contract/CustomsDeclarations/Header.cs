using System.Text.Json.Serialization;

namespace BtmsGateway.Contract.CustomsDeclarations;

public record Header
{
    [JsonPropertyName("entryReference")]
    public string? EntryReference { get; init; }

    [JsonPropertyName("entryVersionNumber")]
    public int? EntryVersionNumber { get; init; }

    [JsonPropertyName("previousVersionNumber")]
    public byte? PreviousVersionNumber { get; set; }

    [JsonPropertyName("declarationUCR")]
    public string? DeclarationUCR { get; set; }

    [JsonPropertyName("declarationType")]
    public string? DeclarationType { get; set; }

    [JsonPropertyName("arrivalDateTime")]
    public ulong? ArrivalDateTime { get; set; }

    [JsonPropertyName("submitterTURN")]
    public ulong? SubmitterTURN { get; set; }

    [JsonPropertyName("declarantId")]
    public string? DeclarantId { get; set; }

    [JsonPropertyName("declarantName")]
    public string? DeclarantName { get; set; }

    [JsonPropertyName("dispatchCountryCode")]
    public string? DispatchCountryCode { get; set; }

    [JsonPropertyName("goodsLocationCode")]
    public string? GoodsLocationCode { get; set; }

    [JsonPropertyName("masterUCR")]
    public string? MasterUCR { get; set; }
}
