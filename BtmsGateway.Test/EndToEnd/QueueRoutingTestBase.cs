using Amazon.SQS;
using Amazon.Extensions.NETCore.Setup;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Amazon.SQS.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace BtmsGateway.Test.EndToEnd;

public abstract class QueueRoutingTestBase : TargetRoutingTestBase
{
    protected abstract string ForkQueueName { get; }
    protected abstract string RouteQueueName { get; }

    private IAmazonSQS SqsClient { get; }
    private IAmazonSimpleNotificationService SnsClient { get; }

    protected QueueRoutingTestBase()
    {
        var awsOptions = this.TestWebServer.Services.GetService<AWSOptions>();
        SqsClient = awsOptions.CreateServiceClient<IAmazonSQS>();
        SnsClient = awsOptions.CreateServiceClient<IAmazonSimpleNotificationService>();

        SetupQueue(ForkQueueName);
        SetupQueue(RouteQueueName);
    }

    private void SetupQueue(string queueName)
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

        var queueAttrs = SqsClient.GetQueueAttributesAsync(queueUrl, new List<string> { "QueueArn" }).Result;

        var subsReq = new SubscribeRequest()
        {
            TopicArn = topicArn,
            Endpoint = queueAttrs.QueueARN,
            Protocol = "sqs",
            Attributes = new()
            {
                { "RawMessageDelivery", "true"}
            }
        };

        SnsClient.SubscribeAsync(subsReq).Wait();
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
}