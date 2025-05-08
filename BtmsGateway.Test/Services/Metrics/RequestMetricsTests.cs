using BtmsGateway.Services.Metrics;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace BtmsGateway.Test.Services.Metrics;

public class RequestMetricsTests : MetricsTestBase
{
    [Fact]
    public void When_message_received_Then_counter_is_incremented()
    {
        var metrics = ServiceProvider.GetRequiredService<IRequestMetrics>();
        var messagesReceivedCollector = GetCollector<long>(MetricsConstants.InstrumentNames.MessagesReceived);

        metrics.MessageReceived("TestMessage1", "/test-request-path-1", "Test Message 1", "Routing1");
        metrics.MessageReceived("TestMessage2", "/test-request-path-2", "Test Message 2", "Routing2");

        var receivedMeasurements = messagesReceivedCollector.GetMeasurementSnapshot();
        receivedMeasurements.Count.Should().Be(2);
        receivedMeasurements[0].Value.Should().Be(1);
        receivedMeasurements[0].ContainsTags(MetricsConstants.RequestTags.MessageType).Should().BeTrue();
        receivedMeasurements[0].Tags[MetricsConstants.RequestTags.MessageType].Should().Be("TestMessage1");
        receivedMeasurements[0].ContainsTags(MetricsConstants.RequestTags.RequestPath).Should().BeTrue();
        receivedMeasurements[0].Tags[MetricsConstants.RequestTags.RequestPath].Should().Be("/test-request-path-1");
        receivedMeasurements[0].ContainsTags(MetricsConstants.RequestTags.Legend).Should().BeTrue();
        receivedMeasurements[0].Tags[MetricsConstants.RequestTags.Legend].Should().Be("Test Message 1");
        receivedMeasurements[0].ContainsTags(MetricsConstants.RequestTags.RouteAction).Should().BeTrue();
        receivedMeasurements[0].Tags[MetricsConstants.RequestTags.RouteAction].Should().Be("Routing1");
        receivedMeasurements[1].Value.Should().Be(1);
        receivedMeasurements[1].ContainsTags(MetricsConstants.RequestTags.MessageType).Should().BeTrue();
        receivedMeasurements[1].Tags[MetricsConstants.RequestTags.MessageType].Should().Be("TestMessage2");
        receivedMeasurements[1].ContainsTags(MetricsConstants.RequestTags.RequestPath).Should().BeTrue();
        receivedMeasurements[1].Tags[MetricsConstants.RequestTags.RequestPath].Should().Be("/test-request-path-2");
        receivedMeasurements[1].ContainsTags(MetricsConstants.RequestTags.Legend).Should().BeTrue();
        receivedMeasurements[1].Tags[MetricsConstants.RequestTags.Legend].Should().Be("Test Message 2");
        receivedMeasurements[1].ContainsTags(MetricsConstants.RequestTags.RouteAction).Should().BeTrue();
        receivedMeasurements[1].Tags[MetricsConstants.RequestTags.RouteAction].Should().Be("Routing2");
    }
}
