using BtmsGateway.Services.Routing;

namespace BtmsGateway.Services.Metrics;

public static class InstanceMetadata
{
    private static ILogger s_logger = null!;

    public static string? InstanceId { get; private set; }

    public static async Task InitAsync(IApiSender apiSender, ILoggerFactory loggerFactory)
    {
        try
        {
            s_logger = loggerFactory.CreateLogger(nameof(InstanceMetadata));

            var ecsMetadata = await apiSender.GetEcsMetadataAsync(CancellationToken.None);

            InstanceId = ecsMetadata?.TaskArn?.Split('/').Last() ?? Guid.NewGuid().ToString();
        }
        catch (Exception ex)
        {
            s_logger.LogWarning(
                ex,
                "Unable to retrieve ECS instance metadata. Configuring instance ID with GUID instead."
            );
            InstanceId = Guid.NewGuid().ToString();
        }
    }
}
