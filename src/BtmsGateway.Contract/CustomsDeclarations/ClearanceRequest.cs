using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace BtmsGateway.Contract.CustomsDeclarations;

[XmlRoot(
    ElementName = "ALVSClearanceRequest",
    Namespace = "http://submitimportdocumenthmrcfacade.types.esb.ws.cara.defra.com",
    IsNullable = false
)]
public record ClearanceRequest : CustomsDeclarationsBase
{
    [JsonPropertyName("header")]
    public required ClearanceRequestHeader Header { get; init; }

    [XmlElement("Item")]
    [JsonPropertyName("items")]
    public Item[] Items { get; init; } = [];
}
