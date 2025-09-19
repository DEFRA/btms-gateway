using BtmsGateway.Services.Metrics;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using NSubstitute;
using NSubstitute.Core;
using Serilog;

namespace BtmsGateway.Test.Services.Metrics;

public class HealthMetricsTests : MetricsTestBase
{
    [Fact]
    public void When_health_report_published_Then_health_metrics_are_reported()
    {
        var metrics = ServiceProvider.GetRequiredService<IHealthMetrics>();
        var reportHealthCollector = GetCollector<int>(MetricsConstants.InstrumentNames.Health);

        var reportEntries = new Dictionary<string, HealthReportEntry>
        {
            {
                "BTMS Gateway Dependency 1",
                new HealthReportEntry(
                    HealthStatus.Unhealthy,
                    description: "Health report 1",
                    TimeSpan.Zero,
                    null,
                    new Dictionary<string, object>
                    {
                        { "topic-arn", "aws_acc:some_topic.fifo" },
                        { "content", "some content" },
                    }
                )
            },
            {
                "BTMS Gateway Dependency 2",
                new HealthReportEntry(
                    HealthStatus.Degraded,
                    description: "Health report 2",
                    TimeSpan.Zero,
                    null,
                    new Dictionary<string, object> { { "queue", "some_queue" } }
                )
            },
            {
                "BTMS Gateway Dependency 3",
                new HealthReportEntry(
                    HealthStatus.Healthy,
                    description: "Health report 3",
                    TimeSpan.Zero,
                    null,
                    new Dictionary<string, object> { { "api", "some_api" } }
                )
            },
        };
        var healthReport = new HealthReport(reportEntries, HealthStatus.Degraded, TimeSpan.FromSeconds(1));

        metrics.ReportHealth(healthReport);

        // 0=Unhealthy, 1=Degraded, 2=Healthy
        var healthMeasurements = reportHealthCollector.GetMeasurementSnapshot();
        healthMeasurements.Count.Should().Be(4);
        healthMeasurements[0].Value.Should().Be(1);
        healthMeasurements[0].ContainsTags(MetricsConstants.HealthTags.Component).Should().BeTrue();
        healthMeasurements[0].ContainsTags("content").Should().BeFalse();
        healthMeasurements[0].Tags[MetricsConstants.HealthTags.Component].Should().Be("BTMS Gateway");
        healthMeasurements[0]
            .Tags[MetricsConstants.HealthTags.Description]
            .Should()
            .Be("Overall health of the BTMS Gateway");

        healthMeasurements[1].Value.Should().Be(0);
        healthMeasurements[1].ContainsTags(MetricsConstants.HealthTags.Component).Should().BeTrue();
        healthMeasurements[1].Tags[MetricsConstants.HealthTags.Component].Should().Be("BTMS Gateway Dependency 1");
        healthMeasurements[1].Tags[MetricsConstants.HealthTags.Description].Should().Be("Health report 1");
        healthMeasurements[1].Tags["topic-arn"].Should().Be("aws_acc:some_topic.fifo");

        healthMeasurements[2].Value.Should().Be(1);
        healthMeasurements[2].ContainsTags(MetricsConstants.HealthTags.Component).Should().BeTrue();
        healthMeasurements[2].Tags[MetricsConstants.HealthTags.Component].Should().Be("BTMS Gateway Dependency 2");
        healthMeasurements[2].Tags[MetricsConstants.HealthTags.Description].Should().Be("Health report 2");
        healthMeasurements[2].Tags["queue"].Should().Be("some_queue");

        healthMeasurements[3].Value.Should().Be(2);
        healthMeasurements[3].ContainsTags(MetricsConstants.HealthTags.Component).Should().BeTrue();
        healthMeasurements[3].Tags[MetricsConstants.HealthTags.Component].Should().Be("BTMS Gateway Dependency 3");
        healthMeasurements[3].Tags[MetricsConstants.HealthTags.Description].Should().Be("Health report 3");
        healthMeasurements[3].Tags["api"].Should().Be("some_api");
    }

    [Fact]
    public void When_health_report_published_and_error_occurs_Then_remaining_health_metrics_are_still_reported()
    {
        var logger = ServiceProvider.GetRequiredService<ILogger>();
        logger
            .When(x => x.Information(Arg.Any<string>(), Arg.Any<int>()))
            .Do(Callback.FirstThrow(new Exception()).Then(_ => { }));
        logger
            .When(x => x.Information(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>()))
            .Do(Callback.FirstThrow(new Exception()).Then(_ => { }));

        var metrics = ServiceProvider.GetRequiredService<IHealthMetrics>();
        var reportHealthCollector = GetCollector<int>(MetricsConstants.InstrumentNames.Health);

        var reportEntries = new Dictionary<string, HealthReportEntry>
        {
            {
                "BTMS Gateway Dependency 1",
                new HealthReportEntry(
                    HealthStatus.Unhealthy,
                    description: "Health report 1",
                    TimeSpan.Zero,
                    null,
                    new Dictionary<string, object> { { "topic-arn", "aws_acc:some_topic.fifo" } }
                )
            },
            {
                "BTMS Gateway Dependency 2",
                new HealthReportEntry(
                    HealthStatus.Degraded,
                    description: "Health report 2",
                    TimeSpan.Zero,
                    null,
                    new Dictionary<string, object> { { "queue", "some_queue" } }
                )
            },
        };
        var healthReport = new HealthReport(reportEntries, HealthStatus.Degraded, TimeSpan.FromSeconds(1));

        metrics.ReportHealth(healthReport);

        var healthMeasurements = reportHealthCollector.GetMeasurementSnapshot();
        healthMeasurements.Count.Should().Be(3);
    }
}
