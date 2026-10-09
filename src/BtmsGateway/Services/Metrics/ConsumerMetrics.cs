using System.Diagnostics;
using System.Diagnostics.Metrics;
using Amazon.CloudWatch.EMF.Model;

namespace BtmsGateway.Services.Metrics;

public interface IConsumerMetrics
{
    void Start(string path, string consumerName, string resourceType, string? subResourceType);
    void Faulted(
        string queueName,
        string consumerName,
        string resourceType,
        string? subResourceType,
        Exception exception
    );
    void Complete(
        string queueName,
        string consumerName,
        double milliseconds,
        string resourceType,
        string? subResourceType
    );
}

public class ConsumerMetrics : IConsumerMetrics
{
    private readonly Histogram<double> consumeDuration;
    private readonly Counter<long> consumeTotal;
    private readonly Counter<long> consumeFaultTotal;
    private readonly Counter<long> consumerInProgress;

    public ConsumerMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MetricsConstants.MetricNames.MeterName);
        consumeTotal = meter.CreateCounter<long>(
            MetricsConstants.InstrumentNames.MessagingConsume,
            Unit.COUNT.ToString(),
            description: "Number of messages consumed"
        );
        consumeFaultTotal = meter.CreateCounter<long>(
            MetricsConstants.InstrumentNames.MessagingConsumeErrors,
            Unit.COUNT.ToString(),
            description: "Number of message consume faults"
        );
        consumerInProgress = meter.CreateCounter<long>(
            MetricsConstants.InstrumentNames.MessagingConsumeActive,
            Unit.COUNT.ToString(),
            description: "Number of consumers in progress"
        );
        consumeDuration = meter.CreateHistogram<double>(
            MetricsConstants.InstrumentNames.MessagingConsumeDuration,
            Unit.MILLISECONDS.ToString(),
            "Elapsed time spent consuming a message, in millis"
        );
    }

    public void Start(string path, string consumerName, string resourceType, string? subResourceType)
    {
        var tagList = BuildTags(path, consumerName, resourceType, subResourceType);

        consumeTotal.Add(1, tagList);
        consumerInProgress.Add(1, tagList);
    }

    public void Faulted(
        string queueName,
        string consumerName,
        string resourceType,
        string? subResourceType,
        Exception exception
    )
    {
        var tagList = BuildTags(queueName, consumerName, resourceType, subResourceType);

        tagList.Add(MetricsConstants.ConsumerTags.ExceptionType, exception.GetType().Name);
        consumeFaultTotal.Add(1, tagList);
    }

    public void Complete(
        string queueName,
        string consumerName,
        double milliseconds,
        string resourceType,
        string? subResourceType
    )
    {
        var tagList = BuildTags(queueName, consumerName, resourceType, subResourceType);

        consumerInProgress.Add(-1, tagList);
        consumeDuration.Record(milliseconds, tagList);
    }

    private static TagList BuildTags(string path, string consumerName, string resourceType, string? subResourceType)
    {
        return new TagList
        {
            { MetricsConstants.ConsumerTags.Service, Process.GetCurrentProcess().ProcessName },
            { MetricsConstants.ConsumerTags.QueueName, path },
            { MetricsConstants.ConsumerTags.ConsumerType, consumerName },
            { MetricsConstants.ConsumerTags.ResourceType, resourceType },
            { MetricsConstants.ConsumerTags.SubResourceType, subResourceType },
        };
    }
}
