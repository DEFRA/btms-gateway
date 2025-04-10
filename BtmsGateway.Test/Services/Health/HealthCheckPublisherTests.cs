using System.Diagnostics.Metrics;
using BtmsGateway.Services.Health;
using BtmsGateway.Services.Metrics;
using FluentAssertions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using NSubstitute;
using Serilog;

namespace BtmsGateway.Test.Services.Health;

public class HealthCheckPublisherTests
{
    [Theory]
    [InlineData(HealthStatus.Healthy, 1, 0, 0)]
    [InlineData(HealthStatus.Degraded, 0, 1, 0)]
    [InlineData(HealthStatus.Unhealthy, 0, 0, 1)]
    public async Task When_publishing_health_report_Then_status_should_be_logged(HealthStatus healthStatus,
        int expectedInfoCalls, int expectedWarningCalls, int expectedErrorCalls)
    {
        var meter = new Meter("test");
        var meterFactory = Substitute.For<IMeterFactory>();
        meterFactory.Create(null!).ReturnsForAnyArgs(meter);
        var metricsHost = Substitute.For<MetricsHost>(meterFactory);

        var logger = Substitute.For<ILogger>();
        var loggedMessage = string.Empty;
        logger.When(x => x.Information(Arg.Any<string>()))
            .Do(message => loggedMessage = message[0].ToString());
        logger.When(x => x.Warning(Arg.Any<string>()))
            .Do(message => loggedMessage = message[0].ToString());
        logger.When(x => x.Error(Arg.Any<string>()))
            .Do(message => loggedMessage = message[0].ToString());

        var sut = new HealthCheckPublisher(metricsHost, logger);

        var healthReport = new HealthReport(
            new Dictionary<string, HealthReportEntry>
            {
                ["route"] = new HealthReportEntry(
                    healthStatus,
                    "Test",
                    TimeSpan.FromSeconds(1),
                    null,
                    new Dictionary<string, object>
                    {
                        { "route", "/test"}
                    })
            },
            healthStatus,
            TimeSpan.FromSeconds(1));

        await sut.PublishAsync(healthReport, CancellationToken.None);

        logger.Received(expectedInfoCalls).Information(Arg.Any<string>());
        logger.Received(expectedWarningCalls).Warning(Arg.Any<string>());
        logger.Received(expectedErrorCalls).Error(Arg.Any<string>());
        loggedMessage.Should().Contain($"\"status\":\"{healthStatus.ToString()}\"");
    }
}