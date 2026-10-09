namespace BtmsGateway.Config;

public static class Environment
{
    public static bool IsDevMode(this WebApplicationBuilder builder) => !builder.Environment.IsProduction();

    public static bool IsDevMode(this WebApplication app) => !app.Environment.IsProduction();
}
