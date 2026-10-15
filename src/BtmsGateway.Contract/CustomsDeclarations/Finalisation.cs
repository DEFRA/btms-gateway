using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace BtmsGateway.Contract.CustomsDeclarations;

[XmlRoot(
    ElementName = "FinalisationNotificationRequest",
    Namespace = "http://notifyfinalisedstatehmrcfacade.types.esb.ws.cara.defra.com",
    IsNullable = false
)]
public record Finalisation
{
    [JsonPropertyName("header")]
    public required FinalisationHeader Header { get; init; }

    [JsonPropertyName("serviceHeader")]
    public required ServiceHeader ServiceHeader { get; init; }
}
