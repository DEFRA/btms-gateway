using Microsoft.Extensions.Options;

namespace BtmsGateway.Utils;

public static class Extensions
{
    public static WebApplicationBuilder ConfigureToType<T>(this WebApplicationBuilder builder, string? sectionName = null) where T : class
    {
        sectionName ??= typeof(T).Name.Replace("Config", "");
        builder.Services.Configure<T>(builder.Configuration.GetSection(sectionName));
        builder.Services.AddSingleton(resolver => resolver.GetRequiredService<IOptions<T>>().Value);
        return builder;
    }
}
