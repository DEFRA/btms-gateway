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
    public required string ActivityEventsTopicName { get; init; }

    [Required]
    public required string SqsArnPrefix { get; init; }

    public int ConsumersPerHost { get; init; } = 20;

    // This default matches Slim Message Bus default of 30
    public int VisibilityTimeout { get; init; } = 30;

    public bool AutoStartConsumers { get; init; } = true;

    public string ResourceEventsDeadLetterQueueName => $"{ResourceEventsQueueName}-deadletter";

    public string ResourceEventsDeadLetterQueueArn => $"{SqsArnPrefix}{ResourceEventsDeadLetterQueueName}";

    [Required]
    public required List<string> Topics { get; init; }
}
