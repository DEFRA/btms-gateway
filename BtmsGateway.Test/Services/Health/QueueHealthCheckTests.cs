using System.Net;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using BtmsGateway.Services.Health;
using FluentAssertions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Serilog;
using Exception = System.Exception;

namespace BtmsGateway.Test.Services.Health;

public class QueueHealthCheckTests
{
    private readonly IAmazonSimpleNotificationService _snsClient;

    private readonly QueueHealthCheck _queueHealthCheck;

    public QueueHealthCheckTests()
    {
        _snsClient = Substitute.For<IAmazonSimpleNotificationService>();

        _queueHealthCheck = new QueueHealthCheck("test", "test-arn", _snsClient, Substitute.For<ILogger>());
    }

    [Theory]
    [InlineData(HttpStatusCode.OK, HealthStatus.Healthy)]
    [InlineData(HttpStatusCode.ServiceUnavailable, HealthStatus.Degraded)]
    public async Task When_checking_communication_with_sns_Then_health_check_result_should_indicate_health(
        HttpStatusCode snsStatusCode,
        HealthStatus expectedHealthStatus
    )
    {
        var attributes = new GetTopicAttributesResponse { HttpStatusCode = snsStatusCode, ContentLength = 0 };

        _snsClient
            .GetTopicAttributesAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ReturnsForAnyArgs(attributes);

        var result = await _queueHealthCheck.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        result.Should().NotBeNull();
        result.Status.Should().Be(expectedHealthStatus);
        result.Exception.Should().BeNull();
        result
            .Data.Should()
            .BeEquivalentTo(
                new Dictionary<string, object>
                {
                    { "topic-arn", "test-arn" },
                    { "content-length", 0 },
                    { "http-status-code", snsStatusCode },
                }
            );
    }

    [Fact]
    public async Task When_checking_communication_with_sns_times_out_Then_health_check_result_should_contain_exception()
    {
        _snsClient
            .GetTopicAttributesAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsyncForAnyArgs(new TaskCanceledException());

        var result = await _queueHealthCheck.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        result.Should().NotBeNull();
        result.Status.Should().Be(HealthStatus.Degraded);
        result.Exception.Should().BeAssignableTo<TimeoutException>();
        result
            .Data.Should()
            .BeEquivalentTo(
                new Dictionary<string, object>
                {
                    { "topic-arn", "test-arn" },
                    {
                        "error",
                        $"The topic check was cancelled, probably because it timed out after {ConfigureHealthChecks.Timeout.TotalSeconds} seconds - "
                    },
                }
            );
    }

    [Fact]
    public async Task When_checking_communication_with_sns_throws_exception_Then_health_check_result_should_contain_exception()
    {
        _snsClient
            .GetTopicAttributesAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsyncForAnyArgs(new Exception("Some error happened"));

        var result = await _queueHealthCheck.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        result.Should().NotBeNull();
        result.Status.Should().Be(HealthStatus.Degraded);
        result.Exception.Should().BeAssignableTo<Exception>();
        result
            .Data.Should()
            .BeEquivalentTo(
                new Dictionary<string, object> { { "topic-arn", "test-arn" }, { "error", "Some error happened - " } }
            );
    }
}
