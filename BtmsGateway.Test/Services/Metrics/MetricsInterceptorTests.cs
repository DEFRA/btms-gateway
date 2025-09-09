using BtmsGateway.Config;
using BtmsGateway.Exceptions;
using BtmsGateway.Extensions;
using BtmsGateway.Services.Metrics;
using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;
using Defra.TradeImportsDataApi.Domain.Events;
using FluentAssertions;
using Microsoft.Extensions.Options;
using NSubstitute;
using SlimMessageBus;

namespace BtmsGateway.Test.Services.Metrics;

public class MetricsInterceptorTests
{
    private readonly Func<Task<object>> pipelineCompletedFunc = async () => await Task.FromResult("OK");

    IConsumerMetrics consumerMetrics = Substitute.For<IConsumerMetrics>();
    IRequestMetrics requestMetrics = Substitute.For<IRequestMetrics>();
    IConsumerContext<CustomsDeclaration> consumerContext = Substitute.For<IConsumerContext<CustomsDeclaration>>();
    IConsumer<ResourceEvent<CustomsDeclaration>> consumer = Substitute.For<
        IConsumer<ResourceEvent<CustomsDeclaration>>
    >();
    MetricsInterceptor<CustomsDeclaration> interceptor;

    public MetricsInterceptorTests()
    {
        var awsSqsOptions = new AwsSqsOptions
        {
            OutboundClearanceDecisionsQueueName = "test-queue",
            OutboundClearanceDecisionsDeadLetterQueueArn = "test-queue-deadletter",
        };
        var options = Substitute.For<IOptions<AwsSqsOptions>>();
        options.Value.Returns(awsSqsOptions);

        var headers = new Dictionary<string, object>
        {
            { MessageBusHeaders.ResourceType, "CustomsDeclaration" },
            { MessageBusHeaders.SubResourceType, "ClearanceDecision" },
        };
        consumerContext.Headers.Returns(headers);
        consumerContext.Path.Returns("test-queue");
        consumerContext.Consumer.Returns(consumer);

        interceptor = new MetricsInterceptor<CustomsDeclaration>(consumerMetrics, requestMetrics, options);
    }

    [Fact]
    public async Task When_message_handled_for_clearance_decision_Then_message_received_is_recorded()
    {
        await interceptor.OnHandle(new CustomsDeclaration(), pipelineCompletedFunc, consumerContext);

        requestMetrics.Received(1).MessageReceived("ClearanceDecision", "test-queue", "BTMS Decision", "Consumer");
    }

    [Fact]
    public async Task When_message_handled_is_not_clearance_decision_Then_message_received_is_not_recorded()
    {
        var headers = new Dictionary<string, object>();
        consumerContext.Headers.Returns(headers);

        await interceptor.OnHandle(new CustomsDeclaration(), pipelineCompletedFunc, consumerContext);

        requestMetrics
            .Received(0)
            .MessageReceived(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task When_message_handled_Then_consumer_metric_is_started_and_completed()
    {
        await interceptor.OnHandle(new CustomsDeclaration(), pipelineCompletedFunc, consumerContext);

        consumerMetrics
            .Received(1)
            .Start("test-queue", consumer.GetType().Name, "CustomsDeclaration", "ClearanceDecision");
        consumerMetrics
            .Received(1)
            .Complete(
                "test-queue",
                consumer.GetType().Name,
                Arg.Any<double>(),
                "CustomsDeclaration",
                "ClearanceDecision"
            );
    }

    [Fact]
    public async Task When_message_handled_and_fault_occurs_Then_consumer_fault_metric_is_recorded()
    {
        var exceptionToThrow = new Exception("Test exception");
        requestMetrics
            .When(mockRequestMetrics =>
                mockRequestMetrics.MessageReceived(
                    Arg.Any<string>(),
                    Arg.Any<string>(),
                    Arg.Any<string>(),
                    Arg.Any<string>()
                )
            )
            .Throw(exceptionToThrow);

        var thrownException = await Assert.ThrowsAsync<Exception>(() =>
            interceptor.OnHandle(new CustomsDeclaration(), pipelineCompletedFunc, consumerContext)
        );

        thrownException.Should().BeSameAs(exceptionToThrow);
        consumerMetrics
            .Received(1)
            .Faulted(
                "test-queue",
                consumer.GetType().Name,
                "CustomsDeclaration",
                "ClearanceDecision",
                exceptionToThrow
            );
    }

    [Fact]
    public async Task When_message_handled_and_409_fault_occurs_Then_consumer_fault_metric_is_not_recorded()
    {
        var exceptionToThrow = new ConflictException("Test 409 exception");
        requestMetrics
            .When(mockRequestMetrics =>
                mockRequestMetrics.MessageReceived(
                    Arg.Any<string>(),
                    Arg.Any<string>(),
                    Arg.Any<string>(),
                    Arg.Any<string>()
                )
            )
            .Throw(exceptionToThrow);

        var thrownException = await Assert.ThrowsAsync<ConflictException>(() =>
            interceptor.OnHandle(new CustomsDeclaration(), pipelineCompletedFunc, consumerContext)
        );

        thrownException.Should().BeSameAs(exceptionToThrow);
        consumerMetrics
            .Received(0)
            .Faulted(
                "test-queue",
                consumer.GetType().Name,
                "CustomsDeclaration",
                "ClearanceDecision",
                exceptionToThrow
            );
    }
}
