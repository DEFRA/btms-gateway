using BtmsGateway.Services.Metrics;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace BtmsGateway.Test.Services.Metrics;

public class ConsumerMetricsTests : MetricsTestBase
{
    [Fact]
    public void When_start_Then_consumer_total_and_in_progress_is_incremented()
    {
        var metrics = ServiceProvider.GetRequiredService<IConsumerMetrics>();
        var consumeTotalCollector = GetCollector<long>(MetricsConstants.InstrumentNames.MessagingConsume);
        var consumerInProgressCollector = GetCollector<long>(MetricsConstants.InstrumentNames.MessagingConsumeActive);

        metrics.Start("decisions-queue", "ClearanceDecisionConsumer", "CustomsDeclaration", "ClearanceDecision");

        var consumerTotalMeasurements = consumeTotalCollector.GetMeasurementSnapshot();
        var consumerInProgressMeasurements = consumerInProgressCollector.GetMeasurementSnapshot();

        consumerTotalMeasurements.Count.Should().Be(1);
        consumerTotalMeasurements[0].Value.Should().Be(1);
        consumerTotalMeasurements[0].ContainsTags(MetricsConstants.ConsumerTags.Service).Should().BeTrue();
        consumerTotalMeasurements[0].ContainsTags(MetricsConstants.ConsumerTags.QueueName).Should().BeTrue();
        consumerTotalMeasurements[0].Tags[MetricsConstants.ConsumerTags.QueueName].Should().Be("decisions-queue");
        consumerTotalMeasurements[0].ContainsTags(MetricsConstants.ConsumerTags.ConsumerType).Should().BeTrue();
        consumerTotalMeasurements[0]
            .Tags[MetricsConstants.ConsumerTags.ConsumerType]
            .Should()
            .Be("ClearanceDecisionConsumer");
        consumerTotalMeasurements[0].ContainsTags(MetricsConstants.ConsumerTags.ResourceType).Should().BeTrue();
        consumerTotalMeasurements[0].Tags[MetricsConstants.ConsumerTags.ResourceType].Should().Be("CustomsDeclaration");
        consumerTotalMeasurements[0].ContainsTags(MetricsConstants.ConsumerTags.SubResourceType).Should().BeTrue();
        consumerTotalMeasurements[0]
            .Tags[MetricsConstants.ConsumerTags.SubResourceType]
            .Should()
            .Be("ClearanceDecision");

        consumerInProgressMeasurements.Count.Should().Be(1);
        consumerInProgressMeasurements[0].Value.Should().Be(1);
        consumerInProgressMeasurements[0].ContainsTags(MetricsConstants.ConsumerTags.Service).Should().BeTrue();
        consumerInProgressMeasurements[0].ContainsTags(MetricsConstants.ConsumerTags.QueueName).Should().BeTrue();
        consumerInProgressMeasurements[0].Tags[MetricsConstants.ConsumerTags.QueueName].Should().Be("decisions-queue");
        consumerInProgressMeasurements[0].ContainsTags(MetricsConstants.ConsumerTags.ConsumerType).Should().BeTrue();
        consumerInProgressMeasurements[0]
            .Tags[MetricsConstants.ConsumerTags.ConsumerType]
            .Should()
            .Be("ClearanceDecisionConsumer");
        consumerInProgressMeasurements[0].ContainsTags(MetricsConstants.ConsumerTags.ResourceType).Should().BeTrue();
        consumerInProgressMeasurements[0]
            .Tags[MetricsConstants.ConsumerTags.ResourceType]
            .Should()
            .Be("CustomsDeclaration");
        consumerInProgressMeasurements[0].ContainsTags(MetricsConstants.ConsumerTags.SubResourceType).Should().BeTrue();
        consumerInProgressMeasurements[0]
            .Tags[MetricsConstants.ConsumerTags.SubResourceType]
            .Should()
            .Be("ClearanceDecision");
    }

    [Fact]
    public void When_faulted_Then_consumer_fault_total_is_incremented()
    {
        var metrics = ServiceProvider.GetRequiredService<IConsumerMetrics>();
        var consumeFaultTotalCollector = GetCollector<long>(MetricsConstants.InstrumentNames.MessagingConsumeErrors);

        metrics.Faulted(
            "decisions-queue",
            "ClearanceDecisionConsumer",
            "CustomsDeclaration",
            "ClearanceDecision",
            new Exception("Test exception")
        );

        var consumerFaultTotalMeasurements = consumeFaultTotalCollector.GetMeasurementSnapshot();

        consumerFaultTotalMeasurements.Count.Should().Be(1);
        consumerFaultTotalMeasurements[0].Value.Should().Be(1);
        consumerFaultTotalMeasurements[0].ContainsTags(MetricsConstants.ConsumerTags.Service).Should().BeTrue();
        consumerFaultTotalMeasurements[0].ContainsTags(MetricsConstants.ConsumerTags.QueueName).Should().BeTrue();
        consumerFaultTotalMeasurements[0].Tags[MetricsConstants.ConsumerTags.QueueName].Should().Be("decisions-queue");
        consumerFaultTotalMeasurements[0].ContainsTags(MetricsConstants.ConsumerTags.ConsumerType).Should().BeTrue();
        consumerFaultTotalMeasurements[0]
            .Tags[MetricsConstants.ConsumerTags.ConsumerType]
            .Should()
            .Be("ClearanceDecisionConsumer");
        consumerFaultTotalMeasurements[0].ContainsTags(MetricsConstants.ConsumerTags.ResourceType).Should().BeTrue();
        consumerFaultTotalMeasurements[0]
            .Tags[MetricsConstants.ConsumerTags.ResourceType]
            .Should()
            .Be("CustomsDeclaration");
        consumerFaultTotalMeasurements[0].ContainsTags(MetricsConstants.ConsumerTags.SubResourceType).Should().BeTrue();
        consumerFaultTotalMeasurements[0]
            .Tags[MetricsConstants.ConsumerTags.SubResourceType]
            .Should()
            .Be("ClearanceDecision");
        consumerFaultTotalMeasurements[0].ContainsTags(MetricsConstants.ConsumerTags.ExceptionType).Should().BeTrue();
        consumerFaultTotalMeasurements[0].Tags[MetricsConstants.ConsumerTags.ExceptionType].Should().Be("Exception");
    }

    [Fact]
    public void When_complete_Then_consumer_in_progress_should_be_decremented_and_duration_recorded()
    {
        var metrics = ServiceProvider.GetRequiredService<IConsumerMetrics>();
        var consumeDurationCollector = GetCollector<double>(MetricsConstants.InstrumentNames.MessagingConsumeDuration);
        var consumerInProgressCollector = GetCollector<long>(MetricsConstants.InstrumentNames.MessagingConsumeActive);

        metrics.Start("decisions-queue", "ClearanceDecisionConsumer", "CustomsDeclaration", "ClearanceDecision");
        metrics.Complete(
            "decisions-queue",
            "ClearanceDecisionConsumer",
            1000,
            "CustomsDeclaration",
            "ClearanceDecision"
        );

        var consumeDurationMeasurements = consumeDurationCollector.GetMeasurementSnapshot();
        var consumerInProgressMeasurements = consumerInProgressCollector.GetMeasurementSnapshot();

        consumerInProgressMeasurements.Count.Should().Be(2);
        consumerInProgressMeasurements[0].Value.Should().Be(1);
        consumerInProgressMeasurements[1].Value.Should().Be(-1);

        consumeDurationMeasurements.Count.Should().Be(1);
        consumeDurationMeasurements[0].Value.Should().Be(1000);
        consumeDurationMeasurements[0].ContainsTags(MetricsConstants.ConsumerTags.Service).Should().BeTrue();
        consumeDurationMeasurements[0].ContainsTags(MetricsConstants.ConsumerTags.QueueName).Should().BeTrue();
        consumeDurationMeasurements[0].Tags[MetricsConstants.ConsumerTags.QueueName].Should().Be("decisions-queue");
        consumeDurationMeasurements[0].ContainsTags(MetricsConstants.ConsumerTags.ConsumerType).Should().BeTrue();
        consumeDurationMeasurements[0]
            .Tags[MetricsConstants.ConsumerTags.ConsumerType]
            .Should()
            .Be("ClearanceDecisionConsumer");
        consumeDurationMeasurements[0].ContainsTags(MetricsConstants.ConsumerTags.ResourceType).Should().BeTrue();
        consumeDurationMeasurements[0]
            .Tags[MetricsConstants.ConsumerTags.ResourceType]
            .Should()
            .Be("CustomsDeclaration");
        consumeDurationMeasurements[0].ContainsTags(MetricsConstants.ConsumerTags.SubResourceType).Should().BeTrue();
        consumeDurationMeasurements[0]
            .Tags[MetricsConstants.ConsumerTags.SubResourceType]
            .Should()
            .Be("ClearanceDecision");
    }
}
