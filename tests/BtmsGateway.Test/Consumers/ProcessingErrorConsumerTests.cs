using System.Net;
using BtmsGateway.Config;
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
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace BtmsGateway.Test.Consumers;

public class ProcessingErrorConsumerTests
{
    private readonly IErrorNotificationSender _errorNotificationSender = Substitute.For<IErrorNotificationSender>();
    private readonly ILogger<ProcessingErrorConsumer> _logger = NullLogger<ProcessingErrorConsumer>.Instance;
    private readonly ProcessingErrorConsumer _consumer;

    private const string Mrn = "24GB123456789AB012";
    private const string CorrelationId = "external-correlation-id";

    private readonly Func<ProcessingError> _alvsProcessingError = () =>
        new()
        {
            Created = DateTime.UtcNow,
            CorrelationId = CorrelationId,
            Errors = [new ErrorItem { Code = "ALVSVAL01", Message = "ALVS Error" }],
        };

    public ProcessingErrorConsumerTests()
    {
        _consumer = new ProcessingErrorConsumer(
            _errorNotificationSender,
            _logger,
            new OptionsWrapper<CdsOptions>(new CdsOptions { Username = "test-username", Password = "test-password" })
        );
    }

    [Fact]
    public async Task When_processing_succeeds_Then_message_should_be_sent()
    {
        var message = new ResourceEvent<ProcessingErrorResource>
        {
            ResourceId = Mrn,
            ResourceType = "ProcessingError",
            Operation = "Created",
            Resource = new ProcessingErrorResource
            {
                ProcessingErrors =
                [
                    new ProcessingError { Created = DateTime.UtcNow.AddSeconds(-10) },
                    _alvsProcessingError(),
                ],
            },
        };

        var sendErrorNotificationResult = new RoutingResult { StatusCode = HttpStatusCode.OK };

        _errorNotificationSender
            .SendErrorNotificationAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<MessagingConstants.MessageSource>(),
                Arg.Any<RoutingResult>(),
                Arg.Any<IHeaderDictionary>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(sendErrorNotificationResult);

        await _consumer.OnHandle(message, CancellationToken.None);

        await _errorNotificationSender
            .Received(1)
            .SendErrorNotificationAsync(
                Mrn,
                errorNotification: Arg.Is<string>(soap =>
                    soap.Contains(Mrn)
                    && soap.Contains("test-username")
                    && soap.Contains("test-password")
                    && soap.Contains("ALVSVAL01")
                ),
                MessagingConstants.MessageSource.Btms,
                RoutingResult.Empty,
                headers: null,
                CorrelationId,
                CancellationToken.None
            );
    }

    [Fact]
    public async Task WhenValidProcessingErrorsSentItOnlyForwardsALVSVALErrors()
    {
        var message = new ResourceEvent<ProcessingErrorResource>
        {
            ResourceId = Mrn,
            ResourceType = "ProcessingError",
            Operation = "Created",
            Resource = new ProcessingErrorResource
            {
                ProcessingErrors =
                [
                    new ProcessingError { Created = DateTime.UtcNow.AddSeconds(-10) },
                    new ProcessingError
                    {
                        Created = DateTime.UtcNow,
                        CorrelationId = CorrelationId,
                        Errors =
                        [
                            new ErrorItem { Code = "ALVSVAL01", Message = "ALVS Error" },
                            new ErrorItem { Code = "ERR01", Message = "Schema Error" },
                            new ErrorItem { Code = "ALVSVAL02", Message = "ALVS Error 2" },
                        ],
                    },
                ],
            },
        };

        var sendErrorNotificationResult = new RoutingResult { StatusCode = HttpStatusCode.OK };

        _errorNotificationSender
            .SendErrorNotificationAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<MessagingConstants.MessageSource>(),
                Arg.Any<RoutingResult>(),
                Arg.Any<IHeaderDictionary>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(sendErrorNotificationResult);

        await _consumer.OnHandle(message, CancellationToken.None);

        await _errorNotificationSender
            .Received(1)
            .SendErrorNotificationAsync(
                Mrn,
                errorNotification: Arg.Is<string>(soap =>
                    soap.Contains(Mrn)
                    && soap.Contains("test-username")
                    && soap.Contains("test-password")
                    && soap.Contains("ALVSVAL01")
                    && soap.Contains("ALVSVAL02")
                    && !soap.Contains("ERR01")
                ),
                MessagingConstants.MessageSource.Btms,
                RoutingResult.Empty,
                headers: null,
                CorrelationId,
                CancellationToken.None
            );
    }

    [Fact]
    public async Task WhenValidProcessingErrorsOnlyContainsNonALVSVALErrorsItIsSkipped()
    {
        var message = new ResourceEvent<ProcessingErrorResource>
        {
            ResourceId = Mrn,
            ResourceType = "ProcessingError",
            Operation = "Created",
            Resource = new ProcessingErrorResource
            {
                ProcessingErrors =
                [
                    new ProcessingError { Created = DateTime.UtcNow.AddSeconds(-10) },
                    new ProcessingError
                    {
                        Created = DateTime.UtcNow,
                        CorrelationId = CorrelationId,
                        Errors =
                        [
                            new ErrorItem { Code = "ERR01", Message = "Schema Error" },
                            new ErrorItem { Code = "ERR02", Message = "Another Schema Error" },
                        ],
                    },
                ],
            },
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
                Arg.Any<string>(),
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
                Arg.Any<string>(),
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
                Arg.Any<string>(),
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
                Arg.Any<string>(),
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
            Resource = new ProcessingErrorResource { ProcessingErrors = [_alvsProcessingError()] },
        };

        var sendErrorNotificationResult = new RoutingResult { StatusCode = HttpStatusCode.BadRequest };

        _errorNotificationSender
            .SendErrorNotificationAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<MessagingConstants.MessageSource>(),
                Arg.Any<RoutingResult>(),
                Arg.Any<IHeaderDictionary>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(sendErrorNotificationResult);

        var thrownException = await Assert.ThrowsAsync<ProcessingErrorProcessingException>(() =>
            _consumer.OnHandle(message, CancellationToken.None)
        );
        thrownException.Message.Should().Be("24GB123456789AB012 Failed to process processing error resource event.");
        thrownException.InnerException.Should().BeAssignableTo<ProcessingErrorProcessingException>();
        thrownException.InnerException?.Message.Should().Be("24GB123456789AB012 Failed to send error notification.");
    }

    [Fact]
    public async Task When_sending_to_decision_comparer_returns_conflict_exception_Then_conflict_exception_is_thrown()
    {
        var message = new ResourceEvent<ProcessingErrorResource>
        {
            ResourceId = "24GB123456789AB012",
            ResourceType = "ProcessingError",
            Operation = "Created",
            Resource = new ProcessingErrorResource { ProcessingErrors = [_alvsProcessingError()] },
        };

        _errorNotificationSender
            .SendErrorNotificationAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<MessagingConstants.MessageSource>(),
                Arg.Any<RoutingResult>(),
                Arg.Any<IHeaderDictionary>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>()
            )
            .ThrowsAsync(new ConflictException("Something went wrong"));

        var thrownException = await Assert.ThrowsAsync<ConflictException>(() =>
            _consumer.OnHandle(message, CancellationToken.None)
        );
        thrownException.Message.Should().Be("24GB123456789AB012 Failed to process processing error resource event.");
        thrownException.InnerException.Should().BeAssignableTo<ConflictException>();
        thrownException.InnerException?.Message.Should().Be("Something went wrong");
    }
}
