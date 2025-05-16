using System.Diagnostics.CodeAnalysis;
using BtmsGateway.Services.Metrics;
using BtmsGateway.Services.Routing;

namespace BtmsGateway.Extensions;

[ExcludeFromCodeCoverage]
public static class ApplicationBuilderExtensions
{
    public static async Task InitializeAsync(this IApplicationBuilder builder)
    {
        await InstanceMetadata.InitAsync(
            builder.ApplicationServices.GetRequiredService<IApiSender>(),
            builder.ApplicationServices.GetRequiredService<ILoggerFactory>()
        );
    }
}
