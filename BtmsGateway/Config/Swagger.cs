using System.Diagnostics.CodeAnalysis;
using Microsoft.OpenApi.Models;

namespace BtmsGateway.Config;

[ExcludeFromCodeCoverage]
public static class Swagger
{
    public static void ConfigureSwaggerBuilder(this WebApplicationBuilder builder)
    {
        if (builder.IsSwaggerEnabled())
        {
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("public-v0.1", new OpenApiInfo { Title = "Public API", Version = "v1" });
            });
        }
    }

    public static void ConfigureSwaggerApp(this WebApplication app)
    {
        if (app.IsSwaggerEnabled())
        {
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/public-v0.1/swagger.json", "public");
            });
        }
    }

    private static bool IsSwaggerEnabled(this WebApplicationBuilder builder) =>
        builder.IsDevMode() || builder.Configuration.GetValue<bool>("EnableSwagger");

    private static bool IsSwaggerEnabled(this WebApplication app) =>
        app.IsDevMode() || app.Configuration.GetValue<bool>("EnableSwagger");
}
