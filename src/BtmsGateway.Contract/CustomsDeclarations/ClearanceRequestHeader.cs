using System.Text.Json.Serialization;
using System.Xml.Serialization;
using BtmsGateway.Contract.Json;
using BtmsGateway.Contract.Xml;

namespace BtmsGateway.Contract.CustomsDeclarations;

public record ClearanceRequestHeader : Header
{
    [JsonPropertyName("previousVersionNumber")]
    public int? PreviousVersionNumber { get; set; }

    [XmlElement("DeclarationUCR")]
    [JsonPropertyName("declarationUCR")]
    public string? DeclarationUcr { get; set; }

    [JsonPropertyName("declarationPartNumber")]
    public string? DeclarationPartNumber { get; set; }

    [JsonPropertyName("declarationType")]
    public string? DeclarationType { get; set; }

    [XmlIgnore]
    [JsonPropertyName("arrivalDateTime")]
    [EpochDateTimeJsonConverter]
    public DateTime? ArrivalDateTime { get; set; }

    [XmlElement("ArrivalDateTime")]
    [JsonIgnore]
    public string? ArrivalDateTimeXml
    {
        get => DateTimeUtilities.ToCompactXmlDateTime(ArrivalDateTime);
        set => ArrivalDateTime = DateTimeUtilities.ParseNullableXmlDateTime(value);
    }

    [XmlElement("SubmitterTURN")]
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

    [XmlElement("MasterUCR")]
    [JsonPropertyName("masterUCR")]
    public string? MasterUcr { get; set; }
}
