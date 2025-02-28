using Amazon.SQS;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using Amazon.SQS.Model;

namespace BtmsGateway.Test.EndToEnd;

public abstract class QueueRoutingTestBase : TargetRoutingTestBase
{
    private IAmazonSQS Client { get; }
    protected QueueRoutingTestBase()
    {
        var configuration = GetConfiguration();
        Client = configuration.GetAWSOptions().CreateServiceClient<IAmazonSQS>();
    }

    protected async Task<List<string>> GetMessages(string queueName)
    {
        var queuesResult = await Client.ListQueuesAsync("");
        var queue = queuesResult.QueueUrls.Single(q => q.EndsWith(queueName));
        
        var messages = await GetMessagesRecursiveRetry(queue);
        
        return messages.Select(m => JsonSerializer.Deserialize<MessageWrapper>(m.Body).Message).ToList();
    }

    private async Task<List<Message>> GetMessagesRecursiveRetry(string queueUrl)
    {
        List<Message> output = null;
        int retries = 0;

        while (output == null)
        {
            if(retries++ > 30) throw new TimeoutException();
            
            var messagesResponse = await Client.ReceiveMessageAsync(queueUrl);
            if (messagesResponse.Messages.Any())
            {
                output = messagesResponse.Messages;
            }
            else
            {
                Thread.Sleep(1000);
            }
        }
        
        return output;
    }
    
    static IConfiguration GetConfiguration()
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile(Path.Combine("EndToEnd", "Settings", "localstack.json"));

        return builder.Build();
    }

    public class MessageWrapper
    {
        public string MessageId { get; set; }
        public string Message { get; set; } 
    }
}