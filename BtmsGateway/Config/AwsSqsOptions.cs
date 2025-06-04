using System.ComponentModel.DataAnnotations;

namespace BtmsGateway.Config;

public class AwsSqsOptions
{
    public const string SectionName = nameof(AwsSqsOptions);

    [Required]
    public required string OutboundClearanceDecisionsQueueName { get; set; }

    public int ConsumersPerHost { get; init; } = 20;
}
