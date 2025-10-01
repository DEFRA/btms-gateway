using System.ComponentModel.DataAnnotations;

namespace BtmsGateway.Config;

public class AwsSqsOptions
{
    public const string SectionName = nameof(AwsSqsOptions);

    [Required]
    public required string ResourceEventsQueueName { get; set; }

    [Required]
    public required string SqsArnPrefix { get; set; }

    public int ConsumersPerHost { get; init; } = 20;

    public bool AutoStartConsumers { get; init; } = true;

    public string ResourceEventsDeadLetterQueueName => $"{ResourceEventsQueueName}-deadletter";
    public string ResourceEventsDeadLetterQueueArn => $"{SqsArnPrefix}{ResourceEventsDeadLetterQueueName}";
}
