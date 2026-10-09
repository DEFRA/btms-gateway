using System.Text.Json.Serialization;
using BtmsGateway.Contract.Json;

namespace BtmsGateway.Contract.CustomsDeclarations;

public record ClearanceRequestPostResult
{
    [JsonPropertyName("xmlSchemaVersion")]
    public string? XmlSchemaVersion { get; set; }

    [JsonPropertyName("sendingDate")]
    [EpochDateTimeJsonConverter]
    public DateTime? SendingDate { get; set; }

    [JsonPropertyName("operationCode")]
    public int? OperationCode { get; set; }

    [JsonPropertyName("requestIdentifier")]
    public string? RequestIdentifier { get; set; }
}
