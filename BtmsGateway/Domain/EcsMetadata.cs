using System.Text.Json.Serialization;

namespace BtmsGateway.Domain;

public class EcsMetadata
{
    [property: JsonPropertyName("TaskARN")]
    public string? TaskArn { get; set; }
}
