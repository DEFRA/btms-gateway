using System.Text.RegularExpressions;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using BtmsGateway.Utils;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace BtmsGateway.Services.Health;

public class TopicHealthCheck(
    string name,
    string topicArn,
    IAmazonSimpleNotificationService snsClient,
    ILogger<TopicHealthCheck> logger
) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = new()
    )
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(ConfigureHealthChecks.Timeout);

        Exception? exception = null;
        GetTopicAttributesResponse? attributes = null;
        try
        {
            attributes = await snsClient.GetTopicAttributesAsync(topicArn, cancellationToken);
        }
        catch (TaskCanceledException ex)
        {
            logger.LogWarning(ex, "HEALTH - Retrieving attributes timed out for topic {Arn}", topicArn);
            exception = new TimeoutException(
                $"The topic check was cancelled, probably because it timed out after {ConfigureHealthChecks.Timeout.TotalSeconds} seconds"
            );
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "HEALTH - Retrieving attributes failed for topic {Arn}", topicArn);
            exception = ex;
        }

        var healthStatus = HealthStatus.Healthy;
        var data = new Dictionary<string, object> { { "topic-arn", topicArn } };
        if (attributes != null)
        {
            if (!attributes.HttpStatusCode.IsSuccessStatusCode())
                healthStatus = HealthStatus.Unhealthy;

            data.Add("content-length", attributes.ContentLength);
            data.Add("http-status-code", attributes.HttpStatusCode);
        }

        if (exception != null)
        {
            healthStatus = HealthStatus.Unhealthy;
            data.Add("error", $"{exception.Message} - {exception.InnerException?.Message}");
        }

        return new HealthCheckResult(
            status: healthStatus,
            description: $"Queue route: {string.Join(' ', Regex.Matches(name, "[A-Z][a-z]+", RegexOptions.None, TimeSpan.FromMilliseconds(200)))}",
            exception: exception,
            data: data
        );
    }
}
