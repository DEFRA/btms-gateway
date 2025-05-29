using System.Diagnostics.CodeAnalysis;
using BtmsGateway.Config;
using BtmsGateway.Extensions;
using BtmsGateway.Middleware;
using BtmsGateway.Services.Checking;
using BtmsGateway.Services.Health;
using BtmsGateway.Services.Metrics;
using BtmsGateway.Services.Routing;
using BtmsGateway.Utils;
using BtmsGateway.Utils.Logging;
using Elastic.Serilog.Enrichers.Web;
using Microsoft.AspNetCore.HeaderPropagation;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Serilog;
using Serilog.Core;
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
    var logger = ConfigureLoggingAndTracing(builder);

    builder.Services.AddCustomTrustStore(logger);
    builder.AddServices(logger);
    builder.AddCustomHealthChecks(healthCheckConfig, routingConfig);
    builder.ConfigureSwaggerBuilder();
}

[ExcludeFromCodeCoverage]
static Logger ConfigureLoggingAndTracing(WebApplicationBuilder builder)
{
    var traceIdHeader = builder.Configuration.GetValue<string>("TraceHeader");
    builder.Services.AddHttpContextAccessor();

    builder.Services.TryAddSingleton<ITraceContextAccessor, TraceContextAccessor>();
    builder.Services.AddOptions<TraceHeader>().Bind(builder.Configuration).ValidateDataAnnotations().ValidateOnStart();
    builder.Services.AddTracingForConsumers();
    builder.Services.AddOperationalMetrics();

    builder.Services.AddSingleton<IConfigureOptions<HeaderPropagationOptions>>(sp =>
    {
        var traceHeader = sp.GetRequiredService<IOptions<TraceHeader>>().Value;
        return new ConfigureOptions<HeaderPropagationOptions>(options =>
        {
            if (!string.IsNullOrWhiteSpace(traceHeader.Name))
                options.Headers.Add(traceHeader.Name);
        });
    });
    builder.Services.TryAddSingleton<HeaderPropagationValues>();

    builder.Logging.ClearProviders();
    var httpAccessor = builder.Services.GetHttpContextAccessor();
    var loggerConfiguration = new LoggerConfiguration()
        .ReadFrom.Configuration(builder.Configuration)
        .Enrich.WithEcsHttpContext(httpAccessor)
        .Enrich.FromLogContext()
        .Enrich.With(new TraceContextEnricher())
        .Enrich.With<LogLevelMapper>()
        .Enrich.WithProperty("service.version", Environment.GetEnvironmentVariable("SERVICE_VERSION"));

    if (traceIdHeader != null)
    {
        loggerConfiguration.Enrich.WithCorrelationId(traceIdHeader);
    }

    builder
        .Services.AddOptions<MessageLoggingOptions>()
        .BindConfiguration(MessageLoggingOptions.SectionName)
        .ValidateDataAnnotations();

    var logger = loggerConfiguration.CreateLogger();
    builder.Logging.AddSerilog(logger);
    logger.Information("Starting application");
    return logger;
}

[ExcludeFromCodeCoverage]
static WebApplication ConfigureWebApplication(WebApplication app)
{
    app.UseEmfExporter();
    app.UseHttpLogging();
    app.UseMiddleware<RoutingInterceptor>();
    app.UseCustomHealthChecks();
    app.UseCheckRoutesEndpoints();

    app.ConfigureSwaggerApp();

    return app;
}
