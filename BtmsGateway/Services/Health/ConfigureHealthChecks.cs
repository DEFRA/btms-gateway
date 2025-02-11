using System.Diagnostics.CodeAnalysis;
using BtmsGateway.Services.Checking;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace BtmsGateway.Services.Health;

[ExcludeFromCodeCoverage]
public static class ConfigureHealthChecks
{
    public static void AddCustomHealthChecks(this WebApplicationBuilder builder, HealthCheckConfig? healthCheckConfig)
    {
        builder.Services.AddHealthChecks()
                        .AddResourceUtilizationHealthCheck()
                        .AddTypeActivatedChecks(healthCheckConfig);
        builder.Services.Configure<HealthCheckPublisherOptions>(options => options.Delay = TimeSpan.FromSeconds(30));
        builder.Services.AddSingleton<IHealthCheckPublisher, HealthCheckPublisher>();
    }
    
    private static void AddTypeActivatedChecks(this IHealthChecksBuilder builder, HealthCheckConfig? healthCheckConfig)
    {
        if (healthCheckConfig == null || healthCheckConfig.AutomatedHealthCheckDisabled) return;

        foreach (var healthCheck in healthCheckConfig.Urls.Where(x => x.Value.IncludeInAutomatedHealthCheck))
        {
            builder.AddTypeActivatedCheck<RouteHealthCheck>(healthCheck.Key, failureStatus: HealthStatus.Unhealthy, tags: ["Route", "HTTP"], args: [healthCheck.Key, healthCheck.Value]);
        }
    }
    
    public static void UseCustomHealthChecks(this WebApplication app)
    {
        app.MapHealthChecks("/health", new HealthCheckOptions
        {
            ResponseWriter = (context, healthReport) =>
            {
                context.Response.ContentType = "application/json; charset=utf-8";
                return context.Response.WriteAsync(HealthCheckWriter.WriteHealthStatusAsJson(healthReport, excludeHealthy:false, indented:true));
            }
        });
    }
}