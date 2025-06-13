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

    [Fact]
    public void When_message_successfully_sent_Then_counter_is_incremented()
    {
        var metrics = ServiceProvider.GetRequiredService<IRequestMetrics>();
        var messagesSuccessfullySentCollector = GetCollector<long>(
            MetricsConstants.InstrumentNames.MessagesSuccessfullySent
        );

        metrics.MessageSuccessfullySent("TestMessage1", "/test-request-path-1", "Test Message 1", "Routing1");
        metrics.MessageSuccessfullySent("TestMessage2", "/test-request-path-2", "Test Message 2", "Routing2");

        var sentMeasurements = messagesSuccessfullySentCollector.GetMeasurementSnapshot();
        sentMeasurements.Count.Should().Be(2);
        sentMeasurements[0].Value.Should().Be(1);
        sentMeasurements[0].ContainsTags(MetricsConstants.RequestTags.MessageType).Should().BeTrue();
        sentMeasurements[0].Tags[MetricsConstants.RequestTags.MessageType].Should().Be("TestMessage1");
        sentMeasurements[0].ContainsTags(MetricsConstants.RequestTags.RequestPath).Should().BeTrue();
        sentMeasurements[0].Tags[MetricsConstants.RequestTags.RequestPath].Should().Be("/test-request-path-1");
        sentMeasurements[0].ContainsTags(MetricsConstants.RequestTags.Legend).Should().BeTrue();
        sentMeasurements[0].Tags[MetricsConstants.RequestTags.Legend].Should().Be("Test Message 1");
        sentMeasurements[0].ContainsTags(MetricsConstants.RequestTags.RouteAction).Should().BeTrue();
        sentMeasurements[0].Tags[MetricsConstants.RequestTags.RouteAction].Should().Be("Routing1");
        sentMeasurements[1].Value.Should().Be(1);
        sentMeasurements[1].ContainsTags(MetricsConstants.RequestTags.MessageType).Should().BeTrue();
        sentMeasurements[1].Tags[MetricsConstants.RequestTags.MessageType].Should().Be("TestMessage2");
        sentMeasurements[1].ContainsTags(MetricsConstants.RequestTags.RequestPath).Should().BeTrue();
        sentMeasurements[1].Tags[MetricsConstants.RequestTags.RequestPath].Should().Be("/test-request-path-2");
        sentMeasurements[1].ContainsTags(MetricsConstants.RequestTags.Legend).Should().BeTrue();
        sentMeasurements[1].Tags[MetricsConstants.RequestTags.Legend].Should().Be("Test Message 2");
        sentMeasurements[1].ContainsTags(MetricsConstants.RequestTags.RouteAction).Should().BeTrue();
        sentMeasurements[1].Tags[MetricsConstants.RequestTags.RouteAction].Should().Be("Routing2");
    }

    [Fact]
    public void When_request_completed_Then_requests_counter_is_incremented_and_request_duration_metric_is_emitted()
    {
        var metrics = ServiceProvider.GetRequiredService<IRequestMetrics>();
        var requestReceivedCollector = GetCollector<long>(MetricsConstants.InstrumentNames.RequestReceived);
        var requestDurationCollector = GetCollector<double>(MetricsConstants.InstrumentNames.RequestDuration);

        metrics.RequestCompleted("/test-request-path-1", "POST", 204, 100);
        metrics.RequestCompleted("/test-request-path-2", "POST", 204, 200);

        var receivedMeasurements = requestReceivedCollector.GetMeasurementSnapshot();
        receivedMeasurements.Count.Should().Be(2);
        receivedMeasurements[0].Value.Should().Be(1);
        receivedMeasurements[0].ContainsTags(MetricsConstants.RequestTags.RequestPath).Should().BeTrue();
        receivedMeasurements[0].Tags[MetricsConstants.RequestTags.RequestPath].Should().Be("/test-request-path-1");
        receivedMeasurements[0].ContainsTags(MetricsConstants.RequestTags.HttpMethod).Should().BeTrue();
        receivedMeasurements[0].Tags[MetricsConstants.RequestTags.HttpMethod].Should().Be("POST");
        receivedMeasurements[0].ContainsTags(MetricsConstants.RequestTags.StatusCode).Should().BeTrue();
        receivedMeasurements[0].Tags[MetricsConstants.RequestTags.StatusCode].Should().Be(204);
        receivedMeasurements[1].Value.Should().Be(1);
        receivedMeasurements[1].ContainsTags(MetricsConstants.RequestTags.RequestPath).Should().BeTrue();
        receivedMeasurements[1].Tags[MetricsConstants.RequestTags.RequestPath].Should().Be("/test-request-path-2");
        receivedMeasurements[1].ContainsTags(MetricsConstants.RequestTags.HttpMethod).Should().BeTrue();
        receivedMeasurements[1].Tags[MetricsConstants.RequestTags.HttpMethod].Should().Be("POST");
        receivedMeasurements[1].ContainsTags(MetricsConstants.RequestTags.StatusCode).Should().BeTrue();
        receivedMeasurements[1].Tags[MetricsConstants.RequestTags.StatusCode].Should().Be(204);

        var durationMeasurements = requestDurationCollector.GetMeasurementSnapshot();
        durationMeasurements.Count.Should().Be(2);
        durationMeasurements[0].Value.Should().Be(100);
        durationMeasurements[0].ContainsTags(MetricsConstants.RequestTags.RequestPath).Should().BeTrue();
        durationMeasurements[0].Tags[MetricsConstants.RequestTags.RequestPath].Should().Be("/test-request-path-1");
        durationMeasurements[0].ContainsTags(MetricsConstants.RequestTags.HttpMethod).Should().BeTrue();
        durationMeasurements[0].Tags[MetricsConstants.RequestTags.HttpMethod].Should().Be("POST");
        durationMeasurements[0].ContainsTags(MetricsConstants.RequestTags.StatusCode).Should().BeTrue();
        durationMeasurements[0].Tags[MetricsConstants.RequestTags.StatusCode].Should().Be(204);
        durationMeasurements[1].Value.Should().Be(200);
        durationMeasurements[1].ContainsTags(MetricsConstants.RequestTags.RequestPath).Should().BeTrue();
        durationMeasurements[1].Tags[MetricsConstants.RequestTags.RequestPath].Should().Be("/test-request-path-2");
        durationMeasurements[1].ContainsTags(MetricsConstants.RequestTags.HttpMethod).Should().BeTrue();
        durationMeasurements[1].Tags[MetricsConstants.RequestTags.HttpMethod].Should().Be("POST");
        durationMeasurements[1].ContainsTags(MetricsConstants.RequestTags.StatusCode).Should().BeTrue();
        durationMeasurements[1].Tags[MetricsConstants.RequestTags.StatusCode].Should().Be(204);
    }
    
    [Fact]
    public void When_request_faulted_Then_faulted_metric_is_emitted()
    {
        var metrics = ServiceProvider.GetRequiredService<IRequestMetrics>();
        var faultedCollector = GetCollector<long>(MetricsConstants.InstrumentNames.RequestFaulted);

        metrics.RequestFaulted("/test-request-path-1", "POST", 500, new Exception("Test"));

        var faultedMeasurements = faultedCollector.GetMeasurementSnapshot();
        faultedMeasurements.Count.Should().Be(1);
        faultedMeasurements[0].Value.Should().Be(1);
        faultedMeasurements[0].ContainsTags(MetricsConstants.RequestTags.RequestPath).Should().BeTrue();
        faultedMeasurements[0].Tags[MetricsConstants.RequestTags.RequestPath].Should().Be("/test-request-path-1");
        faultedMeasurements[0].ContainsTags(MetricsConstants.RequestTags.HttpMethod).Should().BeTrue();
        faultedMeasurements[0].Tags[MetricsConstants.RequestTags.HttpMethod].Should().Be("POST");
        faultedMeasurements[0].ContainsTags(MetricsConstants.RequestTags.StatusCode).Should().BeTrue();
        faultedMeasurements[0].Tags[MetricsConstants.RequestTags.StatusCode].Should().Be(500);
        faultedMeasurements[0].ContainsTags(MetricsConstants.RequestTags.ExceptionType).Should().BeTrue();
        faultedMeasurements[0].Tags[MetricsConstants.RequestTags.ExceptionType].Should().Be("Exception");
    }
}
