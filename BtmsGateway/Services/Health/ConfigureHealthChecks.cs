using System.Diagnostics.CodeAnalysis;
using BtmsGateway.Services.Checking;
using BtmsGateway.Services.Routing;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace BtmsGateway.Services.Health;

[ExcludeFromCodeCoverage]
public static class ConfigureHealthChecks
{
    public static readonly TimeSpan Timeout = TimeSpan.FromSeconds(15);

    public static void AddCustomHealthChecks(
        this WebApplicationBuilder builder,
        HealthCheckConfig? healthCheckConfig,
        RoutingConfig? routingConfig
    )
    {
        builder
            .Services.AddHealthChecks()
            .AddResourceUtilizationHealthCheck()
            .AddNetworkChecks(healthCheckConfig)
            .AddQueueChecks(routingConfig);
        builder.Services.Configure<HealthCheckPublisherOptions>(options =>
        {
            options.Delay = TimeSpan.FromSeconds(30);
            options.Period = TimeSpan.FromMinutes(5);
        });
        builder.Services.AddSingleton<IHealthCheckPublisher, HealthCheckPublisher>();
    }

    private static IHealthChecksBuilder AddNetworkChecks(
        this IHealthChecksBuilder builder,
        HealthCheckConfig? healthCheckConfig
    )
    {
        if (healthCheckConfig == null || healthCheckConfig.AutomatedHealthCheckDisabled)
            return builder;

        foreach (var healthCheck in healthCheckConfig.Urls.Where(x => x.Value.IncludeInAutomatedHealthCheck))
        {
            builder.AddTypeActivatedCheck<NetworkHealthCheck>(
                healthCheck.Key,
                failureStatus: HealthStatus.Unhealthy,
                args: [healthCheck.Key, healthCheck.Value]
            );
        }

        return builder;
    }

    private static void AddQueueChecks(this IHealthChecksBuilder builder, RoutingConfig? routingConfig)
    {
        if (routingConfig == null || routingConfig.AutomatedHealthCheckDisabled)
            return;

        foreach (
            var queues in routingConfig.NamedLinks.Where(x =>
                x.Value.LinkType == LinkType.Queue && x.Key != "InboundCustomsDeclarationReceivedTopic"
            )
        )
        {
            builder.AddTypeActivatedCheck<QueueHealthCheck>(
                queues.Key,
                failureStatus: HealthStatus.Unhealthy,
                args: [queues.Key, queues.Value.Link]
            );
        }
    }

    public static void UseCustomHealthChecks(this WebApplication app)
    {
        app.MapGet("/health", () => Results.Ok()).AllowAnonymous();
        app.MapHealthChecks(
            "/health-dotnet",
            new HealthCheckOptions
            {
                ResponseWriter = (context, healthReport) =>
                {
                    context.Response.ContentType = "application/json; charset=utf-8";
                    return context.Response.WriteAsync(
                        HealthCheckWriter.WriteHealthStatusAsJson(healthReport, excludeHealthy: false, indented: true)
                    );
                },
            }
        );
    }
}
