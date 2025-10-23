using System.Diagnostics.Metrics;
using BtmsGateway.Services.Health;
using BtmsGateway.Services.Metrics;
using FluentAssertions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using NSubstitute;

namespace BtmsGateway.Test.Services.Health;

public class HealthCheckPublisherTests
{
    [Theory]
    [InlineData(HealthStatus.Healthy, LogLevel.Information)]
    [InlineData(HealthStatus.Degraded, LogLevel.Warning)]
    [InlineData(HealthStatus.Unhealthy, LogLevel.Error)]
    public async Task When_publishing_health_report_Then_status_should_be_logged(
        HealthStatus healthStatus,
        LogLevel logLevel
    )
    {
        var meter = new Meter("test");
        var meterFactory = Substitute.For<IMeterFactory>();
        meterFactory.Create(null!).ReturnsForAnyArgs(meter);
        var metricsHost = Substitute.For<MetricsHost>(meterFactory);
        var healthMetrics = Substitute.For<IHealthMetrics>();

        var logger = new FakeLogger<HealthCheckPublisher>();
        var sut = new HealthCheckPublisher(metricsHost, healthMetrics, logger);

        var healthReport = new HealthReport(
            new Dictionary<string, HealthReportEntry>
            {
                ["route"] = new HealthReportEntry(
                    healthStatus,
                    "Test",
                    TimeSpan.FromSeconds(1),
                    null,
                    new Dictionary<string, object> { { "route", "/test" } }
                ),
            },
            healthStatus,
            TimeSpan.FromSeconds(1)
        );

        await sut.PublishAsync(healthReport, CancellationToken.None);

        logger.LatestRecord.Level.Should().Be(logLevel);
    }
}
