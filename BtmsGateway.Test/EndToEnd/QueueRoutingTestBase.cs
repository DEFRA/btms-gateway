using Amazon.SQS;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using Amazon;
using Amazon.SQS.Model;
using Newtonsoft.Json;
using JsonSerializer = Newtonsoft.Json.JsonSerializer;

namespace BtmsGateway.Test.EndToEnd;

public abstract class QueueRoutingTestBase : TargetRoutingTestBase
{
    private IAmazonSQS Client { get; }
    private readonly string ServiceUrl;
    protected QueueRoutingTestBase()
    {
        var configuration = GetConfiguration();
        var awsOptions = configuration.GetAWSOptions();
        ServiceUrl = awsOptions.DefaultClientConfig.ServiceURL;
        Client = awsOptions.CreateServiceClient<IAmazonSQS>();
    }

    protected async Task<List<string>> GetMessages(string queueName)
    {
        var queueUrl = Path.Combine(ServiceUrl, "000000000000", queueName);

        try
        {
            var messages = await GetMessagesRecursiveRetry(queueUrl);

            return messages.Select(m => m.Body).ToList();
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to retrieve messages for queue [{queueUrl}]" + Environment.NewLine + JsonConvert.SerializeObject(ex));
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
}