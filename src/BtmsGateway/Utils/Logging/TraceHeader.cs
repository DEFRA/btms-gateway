using System.ComponentModel.DataAnnotations;

namespace BtmsGateway.Utils.Logging;

public class TraceHeader
{
    [ConfigurationKeyName("TraceHeader")]
    [Required]
    public required string Name { get; set; }
}
