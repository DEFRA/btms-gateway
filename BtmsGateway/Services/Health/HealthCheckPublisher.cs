using Microsoft.Extensions.Diagnostics.HealthChecks;
using ILogger = Serilog.ILogger;

namespace BtmsGateway.Services.Health;

public class HealthCheckPublisher(ILogger logger) : IHealthCheckPublisher
{
    public Task PublishAsync(HealthReport report, CancellationToken cancellationToken)
    {
        var healthStatusAsJson = HealthCheckWriter.WriteHealthStatusAsJson(report, excludeHealthy:true, indented:false);
        
        switch (report.Status)
        {
            case HealthStatus.Healthy:
                logger.Information("{HealthReport}", healthStatusAsJson);
                break;
            case HealthStatus.Degraded:
                logger.Warning("{HealthReport}", healthStatusAsJson);
                break;
            case HealthStatus.Unhealthy:
                logger.Error("{HealthReport}", healthStatusAsJson);
                break;
            default:
                logger.Error("Invalid health status");
                break;
        }
        
        return Task.CompletedTask;
    }
}