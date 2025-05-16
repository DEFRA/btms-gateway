using BtmsGateway.Services.Routing;

namespace BtmsGateway.Services.Metrics;

public static class InstanceMetadata
{
    private static ILogger _logger = null!;

    public static string? InstanceId { get; private set; }

    public static async Task InitAsync(IApiSender apiSender, ILoggerFactory loggerFactory)
    {
        try
        {
            _logger = loggerFactory.CreateLogger(nameof(InstanceMetadata));

            var ecsMetadata = await apiSender.GetEcsMetadataAsync(CancellationToken.None);

            var taskArnParts = ecsMetadata?.TaskArn?.Split('/');
            InstanceId = taskArnParts?[^1] ?? Guid.NewGuid().ToString();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Unable to retrieve ECS instance metadata. Configuring instance ID with GUID instead."
            );
            InstanceId = Guid.NewGuid().ToString();
        }
    }
}
