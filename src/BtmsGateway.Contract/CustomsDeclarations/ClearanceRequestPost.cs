using System.Text.Json.Serialization;
using BtmsGateway.Contract.Json;

namespace BtmsGateway.Contract.CustomsDeclarations;

public record ClearanceRequestPost
{
    [JsonPropertyName("xmlSchemaVersion")]
    public string? XmlSchemaVersion { get; set; }

    [JsonPropertyName("userIdentification")]
    public string? UserIdentification { get; set; }

    [JsonPropertyName("userPassword")]
    public string? UserPassword { get; set; }

    [JsonPropertyName("sendingDate")]
    [EpochDateTimeJsonConverter]
    public DateTime? SendingDate { get; set; }

    [JsonPropertyName("alvsClearanceRequest")]
    public ClearanceRequest? AlvsClearanceRequest { get; set; }
}
