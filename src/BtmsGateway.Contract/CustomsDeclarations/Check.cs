using System.Text.Json.Serialization;

namespace BtmsGateway.Contract.CustomsDeclarations;

public record Check
{
    [JsonPropertyName("checkCode")]
    public string? CheckCode { get; set; }

    [JsonPropertyName("departmentCode")]
    public string? DepartmentCode { get; set; }
}
