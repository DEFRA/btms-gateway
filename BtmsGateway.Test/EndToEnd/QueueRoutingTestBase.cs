using Amazon.SQS;
using Amazon.Extensions.NETCore.Setup;
using Amazon.SQS.Model;
using Microsoft.Extensions.DependencyInjection;

namespace BtmsGateway.Test.EndToEnd;

public abstract class QueueRoutingTestBase : TargetRoutingTestBase
{
    private IAmazonSQS Client { get; }
    protected QueueRoutingTestBase()
    {
        var awsOptions = this.TestWebServer.Services.GetService<AWSOptions>();
        Client = awsOptions.CreateServiceClient<IAmazonSQS>();
    }

    protected async Task<List<string>> GetMessages(string queueName)
    {
        var queueUrl = (await Client.GetQueueUrlAsync(queueName)).QueueUrl;

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

            var messagesResponse = await Client.ReceiveMessageAsync(queueUrl);
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