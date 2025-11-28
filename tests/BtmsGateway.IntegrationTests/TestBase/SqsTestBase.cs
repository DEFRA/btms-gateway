using System.Security.Cryptography;
using Amazon.Runtime;
using Amazon.SQS;
using Amazon.SQS.Model;
using BtmsGateway.Extensions;
using BtmsGateway.IntegrationTests.Helpers;
using Xunit.Abstractions;

namespace BtmsGateway.IntegrationTests.TestBase;

public class SqsTestBase(ITestOutputHelper output) : IntegrationTestBase
{
    protected const string InboundCustomsDeclarationProcessorQueueUrl =
        "http://sqs.eu-west-2.127.0.0.1:4566/000000000000/trade_imports_inbound_customs_declarations_processor.fifo";
    protected const string ResourceEventsQueueUrl =
        "http://sqs.eu-west-2.127.0.0.1:4566/000000000000/trade_imports_data_upserted_btms_gateway";
    protected const string ResourceEventsQueueArn =
        "arn:aws:sqs:eu-west-2:000000000000:trade_imports_data_upserted_btms_gateway";
    protected const string ResourceEventsDeadLetterQueueUrl =
        "http://sqs.eu-west-2.127.0.0.1:4566/000000000000/trade_imports_data_upserted_btms_gateway-deadletter";
    protected const string IntegrationTestProfileResourceEventsQueueUrl =
        "http://sqs.eu-west-2.127.0.0.1:4566/000000000000/int_test_trade_imports_data_upserted_btms_gateway";
    protected const string IntegrationTestProfileResourceEventsDeadLetterQueueUrl =
        "http://sqs.eu-west-2.127.0.0.1:4566/000000000000/int_test_trade_imports_data_upserted_btms_gateway-deadletter";

    private readonly AmazonSQSClient _sqsClient = new(
        new BasicAWSCredentials("local", "local"),
        new AmazonSQSConfig
        {
            AuthenticationRegion = "eu-west-2",
            ServiceURL = "http://sqs.eu-west-2.localhost.localstack.cloud:4566",
            Timeout = TimeSpan.FromSeconds(5),
            MaxErrorRetry = 0,
        }
    );

    protected Task<ReceiveMessageResponse> ReceiveMessage(string queueUrl)
    {
        return _sqsClient.ReceiveMessageAsync(
            new ReceiveMessageRequest
            {
                QueueUrl = queueUrl,
                MaxNumberOfMessages = 10,
                WaitTimeSeconds = 0,
            },
            CancellationToken.None
        );
    }

    protected Task<PurgeQueueResponse> PurgeQueue(string queueUrl)
    {
        return _sqsClient.PurgeQueueAsync(new PurgeQueueRequest { QueueUrl = queueUrl });
    }

    protected Task<GetQueueAttributesResponse> GetQueueAttributes(string queueUrl)
    {
        return _sqsClient.GetQueueAttributesAsync(
            new GetQueueAttributesRequest { AttributeNames = ["ApproximateNumberOfMessages"], QueueUrl = queueUrl },
            CancellationToken.None
        );
    }

    protected async Task DrainAllMessages(string queueUrl)
    {
        Assert.True(
            await AsyncWaiter.WaitForAsync(async () =>
            {
                var response = await ReceiveMessage(queueUrl);

                foreach (var message in response.Messages)
                {
                    output?.WriteLine("Drain message: {0} {1}", message.MessageId, message.Body);

                    await _sqsClient.DeleteMessageAsync(
                        new DeleteMessageRequest { QueueUrl = queueUrl, ReceiptHandle = message.ReceiptHandle },
                        CancellationToken.None
                    );
                }

                var approximateNumberOfMessages = (await GetQueueAttributes(queueUrl)).ApproximateNumberOfMessages;

                output?.WriteLine("ApproximateNumberOfMessages: {0}", approximateNumberOfMessages);

                return approximateNumberOfMessages == 0;
            })
        );
    }

    protected async Task<string> SendMessage(
        string messageGroupId,
        string body,
        string queueUrl,
        Dictionary<string, MessageAttributeValue>? messageAttributes = null,
        bool usesFifo = true,
        Dictionary<string, MessageSystemAttributeValue>? messageSystemAttributes = null
    )
    {
        var request = new SendMessageRequest
        {
            MessageAttributes = messageAttributes,
            MessageBody = body,
            MessageDeduplicationId = usesFifo ? RandomNumberGenerator.GetString("abcdefg", 20) : null,
            MessageGroupId = usesFifo ? messageGroupId : null,
            QueueUrl = queueUrl,
        };

        if (messageSystemAttributes is not null)
        {
            request.MessageSystemAttributes = messageSystemAttributes;
        }

        var result = await _sqsClient.SendMessageAsync(request, CancellationToken.None);

        output.WriteLine("Sent {0} to {1}", result.MessageId, queueUrl);

        return result.MessageId;
    }

    protected static Dictionary<string, MessageAttributeValue> WithResourceEventAttributes(
        string resourceType,
        string? subResourceType,
        string resourceId
    )
    {
        var messageAttributes = new Dictionary<string, MessageAttributeValue>
        {
            {
                MessageBusHeaders.ResourceType,
                new MessageAttributeValue { DataType = "String", StringValue = resourceType }
            },
            {
                MessageBusHeaders.ResourceId,
                new MessageAttributeValue { DataType = "String", StringValue = resourceId }
            },
        };

        if (subResourceType != null)
        {
            messageAttributes.Add(
                MessageBusHeaders.SubResourceType,
                new MessageAttributeValue { DataType = "String", StringValue = subResourceType }
            );
        }

        return messageAttributes;
    }

    protected static Dictionary<string, MessageSystemAttributeValue>? WithMessageSystemAttributes(
        string deadLetterQueueSourceArn
    )
    {
        return new Dictionary<string, MessageSystemAttributeValue>
        {
            {
                "DeadLetterQueueSourceArn",
                new MessageSystemAttributeValue { DataType = "String", StringValue = deadLetterQueueSourceArn }
            },
        };
    }
}
