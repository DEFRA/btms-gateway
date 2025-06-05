using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using Amazon;
using Amazon.Runtime;
using Amazon.SQS;
using Amazon.SQS.Model;
using BtmsGateway.Utils;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using ILogger = Serilog.ILogger;

namespace BtmsGateway.Services.Health;

[ExcludeFromCodeCoverage]
public class QueueHealthCheck(string name, string queue, IConfiguration configuration, ILogger logger) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = new()
    )
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(ConfigureHealthChecks.Timeout);

        Exception? checkException = null;
        GetQueueUrlResponse? queueUrlResponse = null;
        try
        {
            using var sqsClient = CreateSqsClient();

            queueUrlResponse = await sqsClient.GetQueueUrlAsync(queue, cancellationToken);
        }
        catch (TaskCanceledException ex)
        {
            logger.Warning(ex, "HEALTH - Retrieving queue URL timed out for queue {Queue}", queue);
            checkException = new TimeoutException(
                $"The queue check was cancelled, probably because it timed out after {ConfigureHealthChecks.Timeout.TotalSeconds} seconds"
            );
        }
        catch (Exception ex)
        {
            logger.Warning(ex, "HEALTH - Retrieving queue URL failed for queue {Queue}", queue);
            checkException = ex;
        }

        var healthStatus = HealthStatus.Healthy;
        var data = new Dictionary<string, object> { { "queue", queue } };
        if (queueUrlResponse != null)
        {
            if (!queueUrlResponse.HttpStatusCode.IsSuccessStatusCode())
                healthStatus = HealthStatus.Unhealthy;

            data.Add("content-length", queueUrlResponse.ContentLength);
            data.Add("http-status-code", queueUrlResponse.HttpStatusCode);
        }

        if (checkException != null)
        {
            healthStatus = HealthStatus.Unhealthy;
            data.Add("error", $"{checkException.Message} - {checkException.InnerException?.Message}");
        }

        return new HealthCheckResult(
            status: healthStatus,
            description: $"Queue route: {string.Join(' ', Regex.Matches(name, "[A-Z][a-z]+", RegexOptions.None, TimeSpan.FromMilliseconds(200)))}",
            exception: checkException,
            data: data
        );
    }

    private AmazonSQSClient CreateSqsClient()
    {
        var clientId = configuration.GetValue<string>("AWS_ACCESS_KEY_ID");
        var clientSecret = configuration.GetValue<string>("AWS_SECRET_ACCESS_KEY");

        if (!string.IsNullOrEmpty(clientSecret) && !string.IsNullOrEmpty(clientId))
        {
            var region = configuration.GetValue<string>("AWS_REGION") ?? "eu-west-2";
            var regionEndpoint = RegionEndpoint.GetBySystemName(region);

            return new AmazonSQSClient(
                new BasicAWSCredentials(clientId, clientSecret),
                new AmazonSQSConfig
                {
                    // https://github.com/aws/aws-sdk-net/issues/1781
                    AuthenticationRegion = region,
                    RegionEndpoint = regionEndpoint,
                    ServiceURL = configuration.GetValue<string>("SQS_Endpoint"),
                }
            );
        }

        return new AmazonSQSClient();
    }
}
