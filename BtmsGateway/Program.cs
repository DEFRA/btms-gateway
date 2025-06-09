using BtmsGateway.Config;
using BtmsGateway.Middleware;
using BtmsGateway.Services.Checking;
using BtmsGateway.Services.Health;
using BtmsGateway.Services.Metrics;
using BtmsGateway.Services.Routing;
using BtmsGateway.Utils;
using BtmsGateway.Utils.Logging;
using Elastic.CommonSchema.Serilog;
using Serilog;

Log.Logger = new LoggerConfiguration().WriteTo.Console(new EcsTextFormatter()).CreateBootstrapLogger();

try
{
    var app = CreateWebApplication(args);
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application start-up failed");
}
finally
{
    await Log.CloseAndFlushAsync();
}

return;

static WebApplication CreateWebApplication(string[] args)
{
    var builder = WebApplication.CreateBuilder(args);

    ConfigureWebApplication(builder);

    return BuildWebApplication(builder);
}

static void ConfigureWebApplication(WebApplicationBuilder builder)
{
    builder.Configuration.AddEnvironmentVariables();
    builder.Configuration.AddIniFile("Properties/local.env", true);
    builder.ConfigureLoggingAndTracing();
    builder.Services.AddCustomTrustStore();
    builder.AddServices();

    var routingConfig = builder.ConfigureToType<RoutingConfig>();
    var healthCheckConfig = builder.ConfigureToType<HealthCheckConfig>();
    builder.AddCustomHealthChecks(healthCheckConfig, routingConfig);
    builder.ConfigureSwaggerBuilder();
}

static WebApplication BuildWebApplication(WebApplicationBuilder builder)
{
    var app = builder.Build();

    app.UseEmfExporter();
    app.UseHttpLogging();
    app.UseMiddleware<RoutingInterceptor>();
    app.UseCustomHealthChecks();
    app.UseCheckRoutesEndpoints();
    app.ConfigureSwaggerApp();

    return app;
}
