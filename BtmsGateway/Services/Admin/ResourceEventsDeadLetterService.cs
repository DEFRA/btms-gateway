using System.Net;
using Amazon.SQS;
using Amazon.SQS.Model;
using BtmsGateway.Config;
using BtmsGateway.Extensions;
using Microsoft.Extensions.Options;

namespace BtmsGateway.Services.Admin;

public interface IResourceEventsDeadLetterService
{
    Task<bool> Redrive(CancellationToken cancellationToken);

    Task<string> Remove(string messageId, CancellationToken cancellationToken);

    Task<bool> Drain(CancellationToken cancellationToken);
}

public class ResourceEventsDeadLetterService(
    IAmazonSQS amazonSqs,
    IOptions<AwsSqsOptions> awsSqsOptions,
    ILogger<ResourceEventsDeadLetterService> logger
) : IResourceEventsDeadLetterService
{
    // Service registered as singleton, therefore, this variable will cache
    private string? _deadLetterQueueUrl;

    public async Task<bool> Redrive(CancellationToken cancellationToken)
    {
        try
        {
            var request = new StartMessageMoveTaskRequest
            {
                SourceArn = awsSqsOptions.Value.ResourceEventsDeadLetterQueueArn,
            };

            var response = await amazonSqs.StartMessageMoveTaskAsync(request, cancellationToken);
            if (response.HttpStatusCode == System.Net.HttpStatusCode.OK)
            {
                logger.LogInformation(
                    "Redrive message move task started with TaskHandle: {TaskHandle}",
                    response.TaskHandle
                );

                return true;
            }

            logger.LogError("Redrive failed with response: {TaskResponse}", response.ToStringExtended());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to initiate start message move task during redrive request");
        }

        return false;
    }

    public async Task<string> Remove(string messageId, CancellationToken cancellationToken)
    {
        try
        {
            var queueUrl = await GetQueueUrl(cancellationToken);

            while (!cancellationToken.IsCancellationRequested)
            {
                var request = new ReceiveMessageRequest
                {
                    QueueUrl = queueUrl,
                    MaxNumberOfMessages = 10,
                    WaitTimeSeconds = 0,
                    VisibilityTimeout = 60,
                };

                var response = await amazonSqs.ReceiveMessageAsync(request, cancellationToken);
                if (response.Messages.Count == 0)
                {
                    return $"No messages found (visibility timeout used was {request.VisibilityTimeout} seconds, therefore wait before retrying)";
                }

                var message = response.Messages.FirstOrDefault(x =>
                    x.MessageId.Equals(messageId, StringComparison.OrdinalIgnoreCase)
                );

                if (message is not null)
                {
                    var result = await amazonSqs.DeleteMessageAsync(
                        new DeleteMessageRequest { QueueUrl = queueUrl, ReceiptHandle = message.ReceiptHandle },
                        cancellationToken
                    );

                    if (result.HttpStatusCode == HttpStatusCode.OK)
                    {
                        logger.LogInformation("Removed message {MessageId} from dead letter queue", messageId);

                        return $"Found message {messageId} and removed";
                    }

                    return $"Found message {messageId} but delete was not successful ({result.HttpStatusCode})";
                }

                await Task.Delay(TimeSpan.FromMilliseconds(200), cancellationToken);
            }

            return "Request was cancelled";
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to remove message from dead letter queue");

            return "Exception, check logs";
        }
    }

    public async Task<bool> Drain(CancellationToken cancellationToken)
    {
        try
        {
            var queueUrl = await GetQueueUrl(cancellationToken);
            var removed = 0;

            while (!cancellationToken.IsCancellationRequested)
            {
                var request = new ReceiveMessageRequest
                {
                    QueueUrl = queueUrl,
                    MaxNumberOfMessages = 10,
                    WaitTimeSeconds = 0,
                    VisibilityTimeout = 60,
                };

                var response = await amazonSqs.ReceiveMessageAsync(request, cancellationToken);
                if (response.Messages.Count == 0)
                {
                    logger.LogInformation("Dead letter queue is empty, {Removed} message(s) removed", removed);

                    return true;
                }

                var deleteRequest = new DeleteMessageBatchRequest
                {
                    QueueUrl = queueUrl,
                    Entries = response
                        .Messages.Select(
                            (message, index) =>
                                new DeleteMessageBatchRequestEntry
                                {
                                    Id = index.ToString(),
                                    ReceiptHandle = message.ReceiptHandle,
                                }
                        )
                        .ToList(),
                };

                var deleteResponse = await amazonSqs.DeleteMessageBatchAsync(deleteRequest, cancellationToken);
                if (deleteResponse.HttpStatusCode != HttpStatusCode.OK || deleteResponse.Failed.Count > 0)
                {
                    logger.LogWarning("Failed to remove a batch of message(s), stopping");

                    return false;
                }

                removed += deleteResponse.Successful.Count;

                logger.LogInformation(
                    "Removed batch of {Total} message(s), total so far {Removed}",
                    deleteResponse.Successful.Count,
                    removed
                );

                await Task.Delay(TimeSpan.FromMilliseconds(200), cancellationToken);
            }

            logger.LogInformation("Drain operation cancelled, total messages removed so far {Removed}", removed);

            return false;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to drain dead letter queue");

            return false;
        }
    }

    private async Task<string> GetQueueUrl(CancellationToken cancellationToken)
    {
        if (_deadLetterQueueUrl is not null)
            return _deadLetterQueueUrl;

        var queueUrlResponse = await amazonSqs.GetQueueUrlAsync(
            new GetQueueUrlRequest { QueueName = awsSqsOptions.Value.ResourceEventsDeadLetterQueueName },
            cancellationToken
        );

        _deadLetterQueueUrl = queueUrlResponse.QueueUrl;

        return _deadLetterQueueUrl;
    }
}
