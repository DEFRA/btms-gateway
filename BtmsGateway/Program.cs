using BtmsGateway.Authentication;
using BtmsGateway.Config;
using BtmsGateway.Endpoints.Admin;
using BtmsGateway.Middleware;
using BtmsGateway.Services.Checking;
using BtmsGateway.Services.Health;
using BtmsGateway.Services.Metrics;
using BtmsGateway.Services.Routing;
using BtmsGateway.Utils;
using BtmsGateway.Utils.Logging;
using Elastic.CommonSchema.Serilog;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
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
    builder.Services.AddProblemDetails();
    builder.Services.AddAuthenticationAuthorization();
}

static WebApplication BuildWebApplication(WebApplicationBuilder builder)
{
    var app = builder.Build();

    app.UseEmfExporter();
    app.UseHttpLogging();
    // Order of middleware matters!
    app.UseMiddleware<MetricsMiddleware>();
    app.UseWhen(
        context => !context.Request.Path.StartsWithSegments("/admin"), //expand this exclusion logic when needed
        builder =>
        {
            builder.UseMiddleware<RoutingInterceptor>();
        }
    );
    app.UseCustomHealthChecks();
    app.UseCheckRoutesEndpoints();
    app.MapAdminEndpoints();
    app.ConfigureSwaggerApp();

    return app;
}

#pragma warning disable S2094
namespace BtmsGateway
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class Program;
}
#pragma warning restore S2094
