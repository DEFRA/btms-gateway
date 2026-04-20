using BtmsGateway.Services.Health;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SlimMessageBus.Host;

namespace BtmsGateway.Tests.Services.Health;

public class CircuitBreakerHealthCheckPublisherTests
{
    private readonly IConsumerControl _consumers = Substitute.For<IConsumerControl>();
    private readonly ILogger<CircuitBreakerHealthCheckPublisher> _logger = Substitute.For<
        ILogger<CircuitBreakerHealthCheckPublisher>
    >();

    private readonly CircuitBreakerHealthCheckPublisher _sut;

    public CircuitBreakerHealthCheckPublisherTests()
    {
        _sut = new CircuitBreakerHealthCheckPublisher(_consumers, _logger);
    }

    private static HealthReport CreateReport(HealthStatus status)
    {
        var entries = new Dictionary<string, HealthReportEntry>
        {
            ["HMRC_CDS"] = new HealthReportEntry(
                status,
                description: null,
                duration: System.TimeSpan.Zero,
                exception: null,
                data: null
            ),
        };

        return new HealthReport(entries, System.TimeSpan.Zero);
    }

    [Fact]
    public async Task PublishAsync_WhenCdsUnhealthy_AndConsumersStarted_ShouldStopConsumers()
    {
        // Arrange
        _consumers.IsStarted.Returns(true);
        var report = CreateReport(HealthStatus.Unhealthy);

        // Act
        await _sut.PublishAsync(report, CancellationToken.None);

        // Assert
        await _consumers.Received(1).Stop();
        await _consumers.DidNotReceive().Start();
    }

    [Fact]
    public async Task PublishAsync_WhenCdsUnhealthy_AndConsumersAlreadyStopped_ShouldDoNothing()
    {
        // Arrange
        _consumers.IsStarted.Returns(false);
        var report = CreateReport(HealthStatus.Unhealthy);

        // Act
        await _sut.PublishAsync(report, CancellationToken.None);

        // Assert
        await _consumers.DidNotReceive().Stop();
        await _consumers.DidNotReceive().Start();
    }

    [Fact]
    public async Task PublishAsync_WhenCdsHealthy_AndConsumersStopped_ShouldStartConsumers()
    {
        // Arrange
        _consumers.IsStarted.Returns(false);
        var report = CreateReport(HealthStatus.Healthy);

        // Act
        await _sut.PublishAsync(report, CancellationToken.None);

        // Assert
        await _consumers.Received(1).Start();
        await _consumers.DidNotReceive().Stop();
    }

    [Fact]
    public async Task PublishAsync_WhenCdsHealthy_AndConsumersAlreadyStarted_ShouldDoNothing()
    {
        // Arrange
        _consumers.IsStarted.Returns(true);
        var report = CreateReport(HealthStatus.Healthy);

        // Act
        await _sut.PublishAsync(report, CancellationToken.None);

        // Assert
        await _consumers.DidNotReceive().Start();
        await _consumers.DidNotReceive().Stop();
    }

    [Fact]
    public async Task PublishAsync_WhenCdsEntryMissing_ShouldDoNothing()
    {
        // Arrange
        var report = new HealthReport(new Dictionary<string, HealthReportEntry>(), System.TimeSpan.Zero);

        // Act
        await _sut.PublishAsync(report, CancellationToken.None);

        // Assert
        await _consumers.DidNotReceive().Start();
        await _consumers.DidNotReceive().Stop();
    }
}
