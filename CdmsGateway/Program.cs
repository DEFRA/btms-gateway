using CdmsGateway.Utils;
using CdmsGateway.Utils.Logging;
using CdmsGateway.Utils.Mongo;
using Serilog;
using Serilog.Core;
using System.Diagnostics.CodeAnalysis;
using CdmsGateway.Config;

//-------- Configure the WebApplication builder------------------//

var app = CreateWebApplication(args);
await app.RunAsync();
return;

[ExcludeFromCodeCoverage]
static WebApplication CreateWebApplication(string[] args)
{
   var builder = WebApplication.CreateBuilder(args);

   ConfigureWebApplication(builder);

   var app = builder.BuildWebApplication();

   return app;
}

[ExcludeFromCodeCoverage]
static void ConfigureWebApplication(WebApplicationBuilder builder)
{
   builder.Configuration.AddEnvironmentVariables();
   builder.Configuration.AddIniFile("Properties/local.env", true);

   var logger = ConfigureLogging(builder);

   // Load certificates into Trust Store - Note must happen before Mongo and Http client connections
   builder.Services.AddCustomTrustStore(logger);

   ConfigureMongoDb(builder);

   builder.ConfigureEndpoints();

   builder.AddServices(logger);
}

[ExcludeFromCodeCoverage]
static Logger ConfigureLogging(WebApplicationBuilder builder)
{
   builder.Logging.ClearProviders();
   var logger = new LoggerConfiguration()
       .ReadFrom.Configuration(builder.Configuration)
       .Enrich.With<LogLevelMapper>()
       .CreateLogger();
   builder.Logging.AddSerilog(logger);
   logger.Information("Starting application");
   return logger;
}

[ExcludeFromCodeCoverage]
static void ConfigureMongoDb(WebApplicationBuilder builder)
{
   builder.Services.AddSingleton<IMongoDbClientFactory>(_ =>
       new MongoDbClientFactory(builder.Configuration.GetValue<string>("Mongo:DatabaseUri")!,
           builder.Configuration.GetValue<string>("Mongo:DatabaseName")!));
}
