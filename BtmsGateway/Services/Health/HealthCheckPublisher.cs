using System.Diagnostics.CodeAnalysis;
using BtmsGateway.Services.Metrics;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using ILogger = Serilog.ILogger;

// ReSharper disable TemplateIsNotCompileTimeConstantProblem

namespace BtmsGateway.Services.Health;

public class HealthCheckPublisher(MetricsHost metricsHost, ILogger logger) : IHealthCheckPublisher
{
    private readonly IMetrics _metrics = metricsHost.GetMetrics();

    [SuppressMessage("SonarLint", "S2629", Justification = "Using string interpolation in logging message template required to get simple JSON into logs")]
    public Task PublishAsync(HealthReport report, CancellationToken cancellationToken)
    {
        var healthStatusAsJson = $"Health: {HealthCheckWriter.WriteHealthStatusAsJson(report, excludeHealthy: true, indented: false)}";

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

        if (report.Status != HealthStatus.Healthy)
        {
            SendMetrics(report);
        }

        return Task.CompletedTask;
    }

    private void SendMetrics(HealthReport report)
    {
        foreach (var entry in report.Entries)
        {
            if (entry.Value.Status != HealthStatus.Healthy)
            {
                var routeLink = (entry.Value.Data.TryGetValue("route", out var route) ? route : null)?.ToString()
                                ?? (entry.Value.Data.TryGetValue("topic-arn", out var topicArn) ? topicArn : null)?.ToString();

                if (routeLink != null)
                    _metrics.RecordRoutingError(routeLink);
            }
        }
    }
}