using Amazon.SQS;
using Amazon.Extensions.NETCore.Setup;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Amazon.SQS.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace BtmsGateway.Test.EndToEnd;

[Trait("Dependence", "localstack")]
public abstract class QueueRoutingTestBase : TargetRoutingTestBase, IDisposable
{
    protected abstract string ForkQueueName { get; }
    protected abstract string RouteQueueName { get; }

    private IAmazonSQS SqsClient { get; }
    private IAmazonSimpleNotificationService SnsClient { get; }

    private string forkTopicArn;
    private string routeTopicArn;

    private string forkQueueUrl;
    private string routeQueueUrl;

    private string forkSubscriptionArn;
    private string routeSubscriptionArn;

    protected QueueRoutingTestBase()
    {
        var awsOptions = this.TestWebServer.Services.GetService<AWSOptions>();
        SqsClient = awsOptions.CreateServiceClient<IAmazonSQS>();
        SnsClient = awsOptions.CreateServiceClient<IAmazonSimpleNotificationService>();

        var forkDeets = SetupQueue(ForkQueueName);
        var routeDeets = SetupQueue(RouteQueueName);

        forkTopicArn = forkDeets.TopicArn;
        forkQueueUrl = forkDeets.QueueUrl;
        forkSubscriptionArn = forkDeets.SubscriptionArn;
        routeTopicArn = routeDeets.TopicArn;
        routeQueueUrl = routeDeets.QueueUrl;
        routeSubscriptionArn = routeDeets.SubscriptionArn;
    }

    private (string TopicArn, string QueueUrl, string SubscriptionArn) SetupQueue(string queueName)
    {
        var topicReq = new CreateTopicRequest()
        {
            Name = queueName,
            Attributes = new()
            {
                { "FifoTopic", "true"}
            }
        };

        var queueReq = new CreateQueueRequest()
        {
            QueueName = queueName,
            Attributes = new()
            {
                { "FifoQueue", "true"}
            }
        };

        var topicArn = SnsClient.CreateTopicAsync(topicReq).Result.TopicArn;

        string queueUrl = SqsClient.CreateQueueAsync(queueReq).Result.QueueUrl;

        var queueArn = SqsClient.GetQueueAttributesAsync(queueUrl, new List<string> { "QueueArn" }).Result.QueueARN;

        var subsReq = new SubscribeRequest()
        {
            TopicArn = topicArn,
            Endpoint = queueArn,
            Protocol = "sqs",
            Attributes = new()
            {
                { "RawMessageDelivery", "true"}
            }
        };

        var subscriptionArn = SnsClient.SubscribeAsync(subsReq).Result.SubscriptionArn;

        return (topicArn, queueUrl, subscriptionArn);
    }

    private void TearDownQueues()
    {
        SnsClient.UnsubscribeAsync(forkSubscriptionArn).Wait();
        SnsClient.UnsubscribeAsync(routeSubscriptionArn).Wait();
        SqsClient.DeleteQueueAsync(forkQueueUrl).Wait();
        SqsClient.DeleteQueueAsync(routeQueueUrl).Wait();
        SnsClient.DeleteTopicAsync(forkTopicArn).Wait();
        SnsClient.DeleteTopicAsync(routeTopicArn).Wait();
    }

    protected async Task<List<string>> GetMessages(string queueName)
    {
        var queueUrl = (await SqsClient.GetQueueUrlAsync(queueName)).QueueUrl;

        try
        {
            var messages = await GetMessagesRecursiveRetry(queueUrl);

            return messages.Select(m => m.Body).ToList();
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to retrieve messages for queue [{queueUrl}]", ex);
        }
    }

    private async Task<List<Message>> GetMessagesRecursiveRetry(string queueUrl)
    {
        List<Message> output = null;
        int retries = 0;

        while (output == null)
        {
            if (retries++ > 30) throw new TimeoutException();

            var messagesResponse = await SqsClient.ReceiveMessageAsync(queueUrl);
            if (messagesResponse.Messages?.Any() == true)
            {
                output = messagesResponse.Messages;
            }
            else
            {
                await Task.Delay(1000);
            }
        }

        return output;
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            TearDownQueues();

            SqsClient?.Dispose();
            SnsClient?.Dispose();
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}