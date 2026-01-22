using System.Net;
using Amazon.SQS;
using Amazon.SQS.Model;
using BtmsGateway.Config;
using BtmsGateway.Services.Admin;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace BtmsGateway.Test.Services.Admin;

public class ResourceEventsDeadLetterServiceTests
{
    private readonly IAmazonSQS _amazonSqs = Substitute.For<IAmazonSQS>();
    private readonly IOptions<AwsSqsOptions> _awsSqsOptions = Substitute.For<IOptions<AwsSqsOptions>>();
    private readonly ILogger<ResourceEventsDeadLetterService> _logger = Substitute.For<
        ILogger<ResourceEventsDeadLetterService>
    >();

    private readonly ResourceEventsDeadLetterService _resourceEventsDeadLetterService;

    private const string ActivityEventsTopicName = "activity_topic";
    private const string QueueName = "outbound_queue";
    private const string QueueNameDeadLetter = "outbound_queue-deadletter";
    private const string ArnPrefix = "arn:aws:sqs:eu-west-2:000000000000:";

    public ResourceEventsDeadLetterServiceTests()
    {
        _awsSqsOptions.Value.Returns(
            new AwsSqsOptions
            {
                ResourceEventsQueueName = QueueName,
                SqsArnPrefix = ArnPrefix,
                ActivityEventsTopicName = ActivityEventsTopicName,
                Topics = ["arn:aws:sns:eu-west-2:000000000000:activity_topic"],
            }
        );

        _resourceEventsDeadLetterService = new ResourceEventsDeadLetterService(_amazonSqs, _awsSqsOptions, _logger);
    }

    [Fact]
    public async Task When_redrive_successful_Then_return_true()
    {
        _amazonSqs
            .StartMessageMoveTaskAsync(
                Arg.Is<StartMessageMoveTaskRequest>(x => x.SourceArn == ArnPrefix + QueueNameDeadLetter)
            )
            .Returns(Task.FromResult(new StartMessageMoveTaskResponse { HttpStatusCode = HttpStatusCode.OK }));

        var result = await _resourceEventsDeadLetterService.Redrive(CancellationToken.None);

        Assert.True(result);
    }

    [Fact]
    public async Task When_redrive_failed_Then_return_false()
    {
        _amazonSqs
            .StartMessageMoveTaskAsync(Arg.Any<StartMessageMoveTaskRequest>())
            .Returns(
                Task.FromResult(
                    new StartMessageMoveTaskResponse { HttpStatusCode = HttpStatusCode.InternalServerError }
                )
            );

        var result = await _resourceEventsDeadLetterService.Redrive(CancellationToken.None);

        Assert.False(result);
    }

    [Fact]
    public async Task When_redrive_throws_exception_Then_return_false()
    {
        _amazonSqs.StartMessageMoveTaskAsync(Arg.Any<StartMessageMoveTaskRequest>()).ThrowsAsync(new Exception("Test"));

        var result = await _resourceEventsDeadLetterService.Redrive(CancellationToken.None);

        Assert.False(result);
    }

    [Fact]
    public async Task When_remove_message_successful_Then_return_as_expected()
    {
        const string messageId = "messageId";
        const string queueUrl = "queueUrl";
        const string receiptHandle = "receiptHandle";
        _amazonSqs
            .GetQueueUrlAsync(Arg.Is<GetQueueUrlRequest>(x => x.QueueName == QueueNameDeadLetter))
            .Returns(Task.FromResult(new GetQueueUrlResponse { QueueUrl = queueUrl }));
        _amazonSqs
            .ReceiveMessageAsync(Arg.Is<ReceiveMessageRequest>(x => x.QueueUrl == queueUrl))
            .Returns(
                Task.FromResult(
                    new ReceiveMessageResponse
                    {
                        Messages = [new Message { MessageId = messageId, ReceiptHandle = receiptHandle }],
                    }
                )
            );
        _amazonSqs
            .DeleteMessageAsync(Arg.Is<DeleteMessageRequest>(x => x.ReceiptHandle == receiptHandle))
            .Returns(Task.FromResult(new DeleteMessageResponse { HttpStatusCode = HttpStatusCode.OK }));

        var result = await _resourceEventsDeadLetterService.Remove(messageId, CancellationToken.None);

        result.Should().Be($"Found message {messageId} and removed");
    }

    [Fact]
    public async Task When_remove_message_unsuccessful_Then_return_as_expected()
    {
        const string messageId = "messageId";
        const string queueUrl = "queueUrl";
        const string receiptHandle = "receiptHandle";
        _amazonSqs
            .GetQueueUrlAsync(Arg.Is<GetQueueUrlRequest>(x => x.QueueName == QueueNameDeadLetter))
            .Returns(Task.FromResult(new GetQueueUrlResponse { QueueUrl = queueUrl }));
        _amazonSqs
            .ReceiveMessageAsync(Arg.Is<ReceiveMessageRequest>(x => x.QueueUrl == queueUrl))
            .Returns(
                Task.FromResult(
                    new ReceiveMessageResponse
                    {
                        Messages = [new Message { MessageId = messageId, ReceiptHandle = receiptHandle }],
                    }
                )
            );
        _amazonSqs
            .DeleteMessageAsync(Arg.Is<DeleteMessageRequest>(x => x.ReceiptHandle == receiptHandle))
            .Returns(
                Task.FromResult(new DeleteMessageResponse { HttpStatusCode = HttpStatusCode.InternalServerError })
            );

        var result = await _resourceEventsDeadLetterService.Remove(messageId, CancellationToken.None);

        result.Should().Be($"Found message {messageId} but delete was not successful (InternalServerError)");
    }

    [Fact]
    public async Task When_remove_message_and_no_messages_on_dlq_Then_return_as_expected()
    {
        const string messageId = "messageId";
        const string queueUrl = "queueUrl";
        _amazonSqs
            .GetQueueUrlAsync(Arg.Is<GetQueueUrlRequest>(x => x.QueueName == QueueNameDeadLetter))
            .Returns(Task.FromResult(new GetQueueUrlResponse { QueueUrl = queueUrl }));
        _amazonSqs
            .ReceiveMessageAsync(Arg.Is<ReceiveMessageRequest>(x => x.QueueUrl == queueUrl))
            .Returns(Task.FromResult(new ReceiveMessageResponse { Messages = [] }));

        var result = await _resourceEventsDeadLetterService.Remove(messageId, CancellationToken.None);

        result
            .Should()
            .Be("No messages found (visibility timeout used was 60 seconds, therefore wait before retrying)");
    }

    [Fact]
    public async Task When_remove_message_and_multiple_receive_calls_return_Then_return_as_expected()
    {
        const string messageId = "messageId";
        const string queueUrl = "queueUrl";
        _amazonSqs
            .GetQueueUrlAsync(Arg.Is<GetQueueUrlRequest>(x => x.QueueName == QueueNameDeadLetter))
            .Returns(Task.FromResult(new GetQueueUrlResponse { QueueUrl = queueUrl }));
        _amazonSqs
            .ReceiveMessageAsync(Arg.Is<ReceiveMessageRequest>(x => x.QueueUrl == queueUrl))
            .Returns(
                Task.FromResult(new ReceiveMessageResponse { Messages = [new Message { MessageId = "unknown1" }] }),
                Task.FromResult(new ReceiveMessageResponse { Messages = [new Message { MessageId = "unknown2" }] }),
                Task.FromResult(new ReceiveMessageResponse { Messages = [] })
            );

        var result = await _resourceEventsDeadLetterService.Remove(messageId, CancellationToken.None);

        result
            .Should()
            .Be("No messages found (visibility timeout used was 60 seconds, therefore wait before retrying)");
    }

    [Fact]
    public async Task When_remove_message_and_exception_Then_return_as_expected()
    {
        const string messageId = "messageId";
        _amazonSqs
            .GetQueueUrlAsync(Arg.Is<GetQueueUrlRequest>(x => x.QueueName == QueueNameDeadLetter))
            .Throws(new Exception());

        var result = await _resourceEventsDeadLetterService.Remove(messageId, CancellationToken.None);

        result.Should().Be("Exception, check logs");
    }

    [Fact]
    public async Task When_drain_successful_Then_return_as_expected()
    {
        const string messageId = "messageId";
        const string queueUrl = "queueUrl";
        const string receiptHandle = "receiptHandle";
        _amazonSqs
            .GetQueueUrlAsync(Arg.Is<GetQueueUrlRequest>(x => x.QueueName == QueueNameDeadLetter))
            .Returns(Task.FromResult(new GetQueueUrlResponse { QueueUrl = queueUrl }));
        _amazonSqs
            .ReceiveMessageAsync(Arg.Is<ReceiveMessageRequest>(x => x.QueueUrl == queueUrl))
            .Returns(
                Task.FromResult(
                    new ReceiveMessageResponse
                    {
                        Messages = [new Message { MessageId = messageId, ReceiptHandle = receiptHandle }],
                    }
                ),
                Task.FromResult(new ReceiveMessageResponse { Messages = [] })
            );
        _amazonSqs
            .DeleteMessageBatchAsync(
                Arg.Is<DeleteMessageBatchRequest>(x =>
                    x.QueueUrl == queueUrl
                    && x.Entries.Count == 1
                    && x.Entries.First().Id == "0"
                    && x.Entries.First().ReceiptHandle == receiptHandle
                )
            )
            .Returns(Task.FromResult(new DeleteMessageBatchResponse { HttpStatusCode = HttpStatusCode.OK }));

        var result = await _resourceEventsDeadLetterService.Drain(CancellationToken.None);

        result.Should().BeTrue();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task When_drain_unsuccessful_Then_return_as_expected(bool statusCode)
    {
        const string messageId = "messageId";
        const string queueUrl = "queueUrl";
        const string receiptHandle = "receiptHandle";
        _amazonSqs
            .GetQueueUrlAsync(Arg.Is<GetQueueUrlRequest>(x => x.QueueName == QueueNameDeadLetter))
            .Returns(Task.FromResult(new GetQueueUrlResponse { QueueUrl = queueUrl }));
        _amazonSqs
            .ReceiveMessageAsync(Arg.Is<ReceiveMessageRequest>(x => x.QueueUrl == queueUrl))
            .Returns(
                Task.FromResult(
                    new ReceiveMessageResponse
                    {
                        Messages = [new Message { MessageId = messageId, ReceiptHandle = receiptHandle }],
                    }
                ),
                Task.FromResult(new ReceiveMessageResponse { Messages = [] })
            );
        _amazonSqs
            .DeleteMessageBatchAsync(
                Arg.Is<DeleteMessageBatchRequest>(x =>
                    x.QueueUrl == queueUrl
                    && x.Entries.Count == 1
                    && x.Entries.First().Id == "0"
                    && x.Entries.First().ReceiptHandle == receiptHandle
                )
            )
            .Returns(
                Task.FromResult(
                    statusCode
                        ? new DeleteMessageBatchResponse { HttpStatusCode = HttpStatusCode.InternalServerError }
                        : new DeleteMessageBatchResponse
                        {
                            HttpStatusCode = HttpStatusCode.OK,
                            Failed = [new BatchResultErrorEntry()],
                        }
                )
            );

        var result = await _resourceEventsDeadLetterService.Drain(CancellationToken.None);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task When_drain_and_exception_Then_return_as_expected()
    {
        _amazonSqs
            .GetQueueUrlAsync(Arg.Is<GetQueueUrlRequest>(x => x.QueueName == QueueNameDeadLetter))
            .Throws(new Exception());

        var result = await _resourceEventsDeadLetterService.Drain(CancellationToken.None);

        result.Should().BeFalse();
    }
}
