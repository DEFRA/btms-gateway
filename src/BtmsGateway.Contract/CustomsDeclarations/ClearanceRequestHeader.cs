using System.Text.Json.Serialization;
using BtmsGateway.Contract.Json;

namespace BtmsGateway.Contract.CustomsDeclarations;

public record ClearanceRequestHeader : Header
{
    [JsonPropertyName("previousVersionNumber")]
    public int? PreviousVersionNumber { get; set; }

    [JsonPropertyName("declarationUCR")]
    public string? DeclarationUcr { get; set; }

    [JsonPropertyName("declarationPartNumber")]
    public string? DeclarationPartNumber { get; set; }

    [JsonPropertyName("declarationType")]
    public string? DeclarationType { get; set; }

    [JsonPropertyName("arrivalDateTime")]
    [EpochDateTimeJsonConverter]
    public DateTime? ArrivalDateTime { get; set; }

    [JsonPropertyName("submitterTURN")]
    public string? SubmitterTurn { get; set; }

    [JsonPropertyName("declarantId")]
    public string? DeclarantId { get; set; }

    [JsonPropertyName("declarantName")]
    public string? DeclarantName { get; set; }

    [JsonPropertyName("dispatchCountryCode")]
    public string? DispatchCountryCode { get; set; }

    [JsonPropertyName("goodsLocationCode")]
    public string? GoodsLocationCode { get; set; }

    [JsonPropertyName("masterUCR")]
    public string? MasterUcr { get; set; }
}
