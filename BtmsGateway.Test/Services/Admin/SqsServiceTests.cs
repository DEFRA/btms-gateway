using System.Net;
using Amazon.SQS;
using Amazon.SQS.Model;
using BtmsGateway.Config;
using BtmsGateway.Services.Admin;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace BtmsGateway.Test.Services.Admin;

public class SqsServiceTests
{
    private readonly IAmazonSQS amazonSqs = Substitute.For<IAmazonSQS>();
    private readonly IOptions<AwsSqsOptions> awsSqsOptions = Substitute.For<IOptions<AwsSqsOptions>>();
    private readonly ILogger<SqsService> logger = Substitute.For<ILogger<SqsService>>();

    private readonly SqsService sqsService;

    public SqsServiceTests()
    {
        awsSqsOptions.Value.Returns(
            new AwsSqsOptions
            {
                OutboundClearanceDecisionsQueueName = "outbound_queue",
                OutboundClearanceDecisionsDeadLetterQueueArn = "deadletter_queue_arn",
            }
        );

        sqsService = new SqsService(amazonSqs, awsSqsOptions, logger);
    }

    [Fact]
    public async Task When_redrive_successful_Then_return_true()
    {
        amazonSqs
            .StartMessageMoveTaskAsync(Arg.Any<StartMessageMoveTaskRequest>())
            .Returns(Task.FromResult(new StartMessageMoveTaskResponse { HttpStatusCode = HttpStatusCode.OK }));

        var result = await sqsService.Redrive(CancellationToken.None);

        Assert.True(result);
    }

    [Fact]
    public async Task When_redrive_failed_Then_return_false()
    {
        amazonSqs
            .StartMessageMoveTaskAsync(Arg.Any<StartMessageMoveTaskRequest>())
            .Returns(
                Task.FromResult(
                    new StartMessageMoveTaskResponse { HttpStatusCode = HttpStatusCode.InternalServerError }
                )
            );

        var result = await sqsService.Redrive(CancellationToken.None);

        Assert.False(result);
    }

    [Fact]
    public async Task When_redrive_throws_exception_Then_return_false()
    {
        amazonSqs.StartMessageMoveTaskAsync(Arg.Any<StartMessageMoveTaskRequest>()).ThrowsAsync(new Exception("Test"));

        var result = await sqsService.Redrive(CancellationToken.None);

        Assert.False(result);
    }
}
