using System.Net;
using BtmsGateway.Consumers;
using BtmsGateway.Domain;
using BtmsGateway.Exceptions;
using BtmsGateway.Services.Routing;
using Defra.TradeImportsDataApi.Domain.Errors;
using Defra.TradeImportsDataApi.Domain.Events;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace BtmsGateway.Test.Consumers;

public class ProcessingErrorConsumerTests
{
    private readonly IErrorNotificationSender _errorNotificationSender = Substitute.For<IErrorNotificationSender>();
    private readonly ILogger<ProcessingErrorConsumer> _logger = NullLogger<ProcessingErrorConsumer>.Instance;
    private readonly ProcessingErrorConsumer _consumer;

    public ProcessingErrorConsumerTests()
    {
        _consumer = new ProcessingErrorConsumer(_errorNotificationSender, _logger);
    }

    [Fact]
    public async Task When_processing_succeeds_Then_message_should_be_sent()
    {
        var message = new ResourceEvent<ProcessingErrorResource>
        {
            ResourceId = "24GB123456789AB012",
            ResourceType = "ProcessingError",
            Operation = "Created",
            Resource = new ProcessingErrorResource { ProcessingErrors = [new ProcessingError()] },
        };

        var sendErrorNotificationResult = new RoutingResult { StatusCode = HttpStatusCode.OK };

        _errorNotificationSender
            .SendErrorNotificationAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<MessagingConstants.MessageSource>(),
                Arg.Any<RoutingResult>(),
                Arg.Any<IHeaderDictionary>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(sendErrorNotificationResult);

        await _consumer.OnHandle(message, CancellationToken.None);

        await _errorNotificationSender
            .Received(1)
            .SendErrorNotificationAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<MessagingConstants.MessageSource>(),
                Arg.Any<RoutingResult>(),
                Arg.Any<IHeaderDictionary>(),
                Arg.Any<CancellationToken>()
            );
    }

    [Fact]
    public async Task When_resource_is_null_Then_message_is_not_sent()
    {
        var message = new ResourceEvent<ProcessingErrorResource>
        {
            ResourceId = "24GB123456789AB012",
            ResourceType = "ProcessingError",
            Operation = "Created",
        };

        await _consumer.OnHandle(message, CancellationToken.None);

        await _errorNotificationSender
            .DidNotReceiveWithAnyArgs()
            .SendErrorNotificationAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<MessagingConstants.MessageSource>(),
                Arg.Any<RoutingResult>(),
                Arg.Any<IHeaderDictionary>(),
                Arg.Any<CancellationToken>()
            );
    }

    [Fact]
    public async Task When_processing_errors_resource_is_null_Then_message_is_not_sent()
    {
        var message = new ResourceEvent<ProcessingErrorResource>
        {
            ResourceId = "24GB123456789AB012",
            ResourceType = "ProcessingError",
            Operation = "Created",
            Resource = null,
        };

        await _consumer.OnHandle(message, CancellationToken.None);

        await _errorNotificationSender
            .DidNotReceiveWithAnyArgs()
            .SendErrorNotificationAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<MessagingConstants.MessageSource>(),
                Arg.Any<RoutingResult>(),
                Arg.Any<IHeaderDictionary>(),
                Arg.Any<CancellationToken>()
            );
    }

    [Fact]
    public async Task When_resource_processing_errors_is_empty_Then_message_is_not_sent()
    {
        var message = new ResourceEvent<ProcessingErrorResource>
        {
            ResourceId = "24GB123456789AB012",
            ResourceType = "ProcessingError",
            Operation = "Created",
            Resource = new ProcessingErrorResource { ProcessingErrors = [] },
        };

        await _consumer.OnHandle(message, CancellationToken.None);

        await _errorNotificationSender
            .DidNotReceiveWithAnyArgs()
            .SendErrorNotificationAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<MessagingConstants.MessageSource>(),
                Arg.Any<RoutingResult>(),
                Arg.Any<IHeaderDictionary>(),
                Arg.Any<CancellationToken>()
            );
    }

    [Fact]
    public async Task When_sending_to_decision_comparer_is_not_successful_Then_exception_is_thrown()
    {
        var message = new ResourceEvent<ProcessingErrorResource>
        {
            ResourceId = "24GB123456789AB012",
            ResourceType = "ProcessingError",
            Operation = "Created",
            Resource = new ProcessingErrorResource { ProcessingErrors = [new ProcessingError()] },
        };

        var sendErrorNotificationResult = new RoutingResult { StatusCode = HttpStatusCode.BadRequest };

        _errorNotificationSender
            .SendErrorNotificationAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<MessagingConstants.MessageSource>(),
                Arg.Any<RoutingResult>(),
                Arg.Any<IHeaderDictionary>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(sendErrorNotificationResult);

        var thrownException = await Assert.ThrowsAsync<ProcessingErrorProcessingException>(() =>
            _consumer.OnHandle(message, CancellationToken.None)
        );
        thrownException.Message.Should().Be("24GB123456789AB012 Failed to process processing error resource event.");
        thrownException.InnerException.Should().BeAssignableTo<ProcessingErrorProcessingException>();
        thrownException
            .InnerException?.Message.Should()
            .Be("24GB123456789AB012 Failed to send error notification to Decision Comparer.");
    }
}
