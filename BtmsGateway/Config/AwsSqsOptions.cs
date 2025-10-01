using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace BtmsGateway.Config;

[SuppressMessage("ReSharper", "AutoPropertyCanBeMadeGetOnly.Global")]
public class AwsSqsOptions
{
    public const string SectionName = nameof(AwsSqsOptions);

    [Required]
    public required string ResourceEventsQueueName { get; init; }

    [Required]
    public required string SqsArnPrefix { get; init; }

    public int ConsumersPerHost { get; init; } = 20;

    public bool AutoStartConsumers { get; init; } = true;

    private string ResourceEventsDeadLetterQueueName => $"{ResourceEventsQueueName}-deadletter";

    public string ResourceEventsDeadLetterQueueArn => $"{SqsArnPrefix}{ResourceEventsDeadLetterQueueName}";
}
