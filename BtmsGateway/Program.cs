using BtmsGateway.Utils;
using BtmsGateway.Utils.Logging;
using Serilog;
using Serilog.Core;
using System.Diagnostics.CodeAnalysis;
using BtmsGateway.Config;
using BtmsGateway.Middleware;
using BtmsGateway.Services.Checking;
using BtmsGateway.Services.Health;
using BtmsGateway.Services.Metrics;
using BtmsGateway.Services.Routing;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Environment = System.Environment;

var app = CreateWebApplication(args);
await app.RunAsync();

[ExcludeFromCodeCoverage]
static WebApplication CreateWebApplication(string[] args)
{
    var builder = WebApplication.CreateBuilder(args);

    BuildWebApplication(builder);

    var app = builder.Build();

    return ConfigureWebApplication(app);
}

[ExcludeFromCodeCoverage]
static void BuildWebApplication(WebApplicationBuilder builder)
{
    builder.Configuration.AddEnvironmentVariables();
    builder.Configuration.AddIniFile("Properties/local.env", true);
    var routingConfig = builder.ConfigureToType<RoutingConfig>();
    var healthCheckConfig = builder.ConfigureToType<HealthCheckConfig>();

    ConfigureTelemetry(builder);
    var logger = ConfigureLogging(builder);

    builder.Services.AddCustomTrustStore(logger);
    builder.AddCustomHealthChecks(healthCheckConfig, routingConfig);
    builder.AddServices(logger);
    builder.ConfigureSwaggerBuilder();
}

[ExcludeFromCodeCoverage]
static Logger ConfigureLogging(WebApplicationBuilder builder)
{
    builder.Logging.ClearProviders();
    var loggerConfiguration = new LoggerConfiguration()
        .ReadFrom.Configuration(builder.Configuration)
        .Enrich.With<LogLevelMapper>()
        .Enrich.WithProperty("service.version", Environment.GetEnvironmentVariable("SERVICE_VERSION"))
        .WriteTo.OpenTelemetry(options =>
        {
            options.LogsEndpoint = builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"];
            options.ResourceAttributes.Add("service.name", "btms-gateway");
        });

    var logger = loggerConfiguration.CreateLogger();
    builder.Logging.AddSerilog(logger);
    logger.Information("Starting application");
    return logger;
}

[ExcludeFromCodeCoverage]
static void ConfigureTelemetry(WebApplicationBuilder builder)
{
    builder.Services.AddOpenTelemetry()
        .WithMetrics(metrics =>
        {
            metrics.AddRuntimeInstrumentation()
                .AddMeter(
                    "Microsoft.AspNetCore.Hosting",
                    "Microsoft.AspNetCore.Server.Kestrel",
                    "System.Net.Http",
                    MetricsHost.MeterName);
        })
        .WithTracing(tracing =>
        {
            tracing.AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddSource(MetricsHost.MeterName);
        })
        .UseOtlpExporter();
}

[ExcludeFromCodeCoverage]
static WebApplication ConfigureWebApplication(WebApplication app)
{
    app.UseEmfExporter();
    app.UseMiddleware<RoutingInterceptor>();
    app.UseCustomHealthChecks();
    app.UseCheckRoutesEndpoints();

    app.ConfigureSwaggerApp();

    return app;
}