namespace BtmsGateway.Config;

public static class Swagger
{
    public static bool IsSwaggerEnabled(this WebApplicationBuilder builder) => builder.IsDevMode() || builder.Configuration.GetValue<bool>("EnableSwagger");
    
    public static bool IsSwaggerEnabled(this WebApplication app) => app.IsDevMode() || app.Configuration.GetValue<bool>("EnableSwagger");
}