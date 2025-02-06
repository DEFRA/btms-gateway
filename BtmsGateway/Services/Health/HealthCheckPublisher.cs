using Microsoft.Extensions.Diagnostics.HealthChecks;
using ILogger = Serilog.ILogger;

// ReSharper disable TemplateIsNotCompileTimeConstantProblem

namespace BtmsGateway.Services.Health;

public class HealthCheckPublisher(ILogger logger) : IHealthCheckPublisher
{
    public Task PublishAsync(HealthReport report, CancellationToken cancellationToken)
    {
        var healthStatusAsJson = HealthCheckWriter.WriteHealthStatusAsJson(report, excludeHealthy:true, indented:false);
        
        switch (report.Status)
        {
            case HealthStatus.Healthy:
                logger.Information(healthStatusAsJson);
                break;
            case HealthStatus.Degraded:
                logger.Warning(healthStatusAsJson);
                break;
            case HealthStatus.Unhealthy:
                logger.Error(healthStatusAsJson);
                break;
            default:
                logger.Error($"{{\"status\":\"Invalid\",\"description\":\"Invalid health status '{report.Status}'\"}}");
                break;
        }
        
        return Task.CompletedTask;
    }
}