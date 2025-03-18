using System.Text.RegularExpressions;
using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using ILogger = Serilog.ILogger;

namespace BtmsGateway.Services.Health;

public class QueueHealthCheck : IHealthCheck
{
    private readonly string _queueName;
    private readonly string _name;
    private readonly AmazonSQSClient _sqsClient;
    private readonly ILogger _logger;
    private readonly string? _queueUrl;
    private readonly Exception? _getQueueUrlException;

    public QueueHealthCheck(string name, string topicArn, AmazonSQSClient sqsClient, ILogger logger)
    {
        _name = name;
        _sqsClient = sqsClient;
        _logger = logger;
        _queueName = topicArn.Split(':')[^1];
        try
        {
            _queueUrl = _sqsClient.GetQueueUrlAsync(_queueName).Result.QueueUrl;
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "HEALTH - Unable to retrieve the queue URL for {QueueName}", _queueName);
            _getQueueUrlException = ex;
        }
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = new())
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(ConfigureHealthChecks.Timeout);

        if (_queueUrl == null)
        {
            _logger.Warning("HEALTH - Retrieving attributes timed out for queue {QueueUrl}", _queueUrl);
            return new HealthCheckResult(status: HealthStatus.Unhealthy, exception: _getQueueUrlException, data: new Dictionary<string, object> { { "name", _queueName } });
        }

        Exception? exception = null;
        GetQueueAttributesResponse? attributes = null;
        try
        {
            attributes = await _sqsClient.GetQueueAttributesAsync(_queueUrl, ["All"], cancellationToken);
        }
        catch (TaskCanceledException)
        {
            _logger.Warning("HEALTH - Retrieving attributes timed out for queue {QueueUrl}", _queueUrl);
            exception = new TimeoutException($"The queue check cas cancelled, probably because it timed out after {ConfigureHealthChecks.Timeout.TotalSeconds} seconds");
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "HEALTH - Retrieving attributes failed for queue {QueueUrl}", _queueUrl);
            exception = ex;
        }

        var healthStatus = attributes != null ? HealthStatus.Healthy : HealthStatus.Degraded;

        var data = attributes == null ? [] : new Dictionary<string, object>
        {
            { "queue-name", _queueName },
            { "queue-url", _queueUrl },
            { "approximate-number-of-messages", attributes.ApproximateNumberOfMessages },
            { "approximate-number-of-messages-delayed", attributes.ApproximateNumberOfMessagesDelayed },
            { "approximate-number-of-messages-not-visible", attributes.ApproximateNumberOfMessagesNotVisible },
            { "content-length", attributes.ContentLength }
        };

        if (exception != null)
        {
            healthStatus = HealthStatus.Unhealthy;
            data.Add("error", $"{exception.Message} - {exception.InnerException?.Message}");
        }

        return new HealthCheckResult(
            status: healthStatus,
            description: $"Queue route: {string.Join(' ', Regex.Matches(_name, "[A-Z][a-z]+", RegexOptions.None, TimeSpan.FromMilliseconds(200)))}",
            exception: exception,
            data: data);
    }
}