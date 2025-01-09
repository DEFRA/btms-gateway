using System.Diagnostics.CodeAnalysis;
using BtmsGateway.Middleware;
using BtmsGateway.Services.Checking;
using BtmsGateway.Services.Routing;
using BtmsGateway.Utils;
using BtmsGateway.Utils.Http;
using FluentValidation;
using ILogger = Serilog.ILogger;

namespace BtmsGateway.Config;

public static class ConfigureWebApp
{
    public static IHttpClientBuilder? HttpRoutedClientWithRetryBuilder { get; private set; }
    public static IHttpClientBuilder? HttpForkedClientWithRetryBuilder { get; private set; }

    [ExcludeFromCodeCoverage]
    public static void AddServices(this WebApplicationBuilder builder, ILogger logger)
    {
        builder.Services.AddSingleton(logger);
        builder.ConfigureToType<RoutingConfig>();
        builder.ConfigureToType<HealthCheckConfig>();

        HttpRoutedClientWithRetryBuilder = builder.Services.AddHttpProxyRoutedClientWithRetry(logger);
        HttpForkedClientWithRetryBuilder = builder.Services.AddHttpProxyForkedClientWithRetry(logger);
        builder.Services.AddHttpProxyClientWithoutRetry(logger);
        
        builder.Services.AddValidatorsFromAssemblyContaining<Program>();

        builder.Services.AddSingleton<IMessageRouter, MessageRouter>();
        builder.Services.AddSingleton<IMessageRoutes, MessageRoutes>();
        builder.Services.AddSingleton<CheckRoutes>();
        builder.Services.AddSingleton<MetricsHost>();
    }

    [ExcludeFromCodeCoverage]
    public static void ConfigureEndpoints(this WebApplicationBuilder builder)
    {
        builder.Services.AddHealthChecks();
    }

    [ExcludeFromCodeCoverage]
    public static WebApplication BuildWebApplication(this WebApplicationBuilder builder)
    {
        var app = builder.Build();

        app.UseMiddleware<RoutingInterceptor>();
   
        app.MapHealthChecks("/health");
        
        app.UseCheckRoutesEndpoints();

        return app;
    }
}