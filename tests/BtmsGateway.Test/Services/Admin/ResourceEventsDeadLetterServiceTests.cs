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

public class ResourceEventsDeadLetterServiceTests
{
    private readonly IAmazonSQS _amazonSqs = Substitute.For<IAmazonSQS>();
    private readonly IOptions<AwsSqsOptions> _awsSqsOptions = Substitute.For<IOptions<AwsSqsOptions>>();
    private readonly ILogger<ResourceEventsDeadLetterService> _logger = Substitute.For<
        ILogger<ResourceEventsDeadLetterService>
    >();

    private readonly ResourceEventsDeadLetterService _resourceEventsDeadLetterService;

    public ResourceEventsDeadLetterServiceTests()
    {
        _awsSqsOptions.Value.Returns(
            new AwsSqsOptions
            {
                ResourceEventsQueueName = "outbound_queue",
                SqsArnPrefix = "arn:aws:sqs:eu-west-2:000000000000:",
            }
        );

        _resourceEventsDeadLetterService = new ResourceEventsDeadLetterService(_amazonSqs, _awsSqsOptions, _logger);
    }

    [Fact]
    public async Task When_redrive_successful_Then_return_true()
    {
        _amazonSqs
            .StartMessageMoveTaskAsync(Arg.Any<StartMessageMoveTaskRequest>())
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
}
