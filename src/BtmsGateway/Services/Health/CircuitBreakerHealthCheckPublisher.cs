using Microsoft.Extensions.Diagnostics.HealthChecks;
using SlimMessageBus.Host;

namespace BtmsGateway.Services.Health;

public partial class CircuitBreakerHealthCheckPublisher(
    IConsumerControl consumers,
    ILogger<CircuitBreakerHealthCheckPublisher> logger
) : IHealthCheckPublisher
{
    public async Task PublishAsync(HealthReport report, CancellationToken cancellationToken)
    {
        if (report.Entries.TryGetValue("HMRC_CDS", out var cdsHealthCheckEntry))
        {
            if (cdsHealthCheckEntry.Status != HealthStatus.Healthy)
            {
                if (consumers.IsStarted)
                {
                    LogCircuitTripped();
                    await consumers.Stop();
                }
            }
            else
            {
                if (!consumers.IsStarted)
                {
                    LogCircuitRestored();
                    await consumers.Start();
                }
            }
        }
    }

    #region Logging

    [LoggerMessage(EventId = 0, Level = LogLevel.Warning, Message = "Circuit breaker tripped. Consumers paused.")]
    private partial void LogCircuitTripped();

    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Circuit breaker restored. Consumers resumed.")]
    private partial void LogCircuitRestored();

    #endregion
}
