using Microsoft.Extensions.Options;

namespace BtmsGateway.Utils;

public static class Extensions
{
    public static WebApplicationBuilder ConfigureToType<T>(this WebApplicationBuilder builder, string sectionName) where T : class
    {
        builder.Services.Configure<T>(builder.Configuration.GetSection(sectionName));
        builder.Services.AddSingleton(resolver => resolver.GetRequiredService<IOptions<T>>().Value);
        return builder;
    }
}
