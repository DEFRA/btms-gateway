using Microsoft.Extensions.Diagnostics.HealthChecks;
using ILogger = Serilog.ILogger;

namespace BtmsGateway.Services.Health;

public class HealthCheckPublisher(ILogger logger) : IHealthCheckPublisher
{
    public Task PublishAsync(HealthReport report, CancellationToken cancellationToken)
    {
        logger.Information("{HealthReport}", HealthCheckWriter.WriteHealthStatusAsJson(report, excludeHealthy:true, indented:false));
        return Task.CompletedTask;
    }
}