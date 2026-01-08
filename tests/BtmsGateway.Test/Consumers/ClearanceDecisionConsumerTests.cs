using System.Net;
using BtmsGateway.Config;
using BtmsGateway.Consumers;
using BtmsGateway.Domain;
using BtmsGateway.Exceptions;
using BtmsGateway.Services.Routing;
using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;
using Defra.TradeImportsDataApi.Domain.Events;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using SlimMessageBus;

namespace BtmsGateway.Test.Consumers;

public class ClearanceDecisionConsumerTests
{
    private readonly IConsumerContext _consumerContext = Substitute.For<IConsumerContext>();
    private readonly IMessageBus _bus = Substitute.For<IMessageBus>();
    private readonly IDecisionSender _decisionSender = Substitute.For<IDecisionSender>();
    private readonly ILogger<ClearanceDecisionConsumer> _logger = NullLogger<ClearanceDecisionConsumer>.Instance;
    private readonly ResourceEvent<CustomsDeclarationEvent> _message;
    private readonly ClearanceDecisionConsumer _consumer;

    private const string Mrn = "24GB123456789AB012";
    private const string CorrelationId = "external-correlation-id";

    public ClearanceDecisionConsumerTests()
    {
        var clearanceDecision = new ClearanceDecision
        {
            CorrelationId = CorrelationId,
            Created = DateTime.Now,
            ExternalVersionNumber = 1,
            DecisionNumber = 1,
            Items =
            [
                new ClearanceDecisionItem
                {
                    ItemNumber = 1,
                    Checks =
                    [
                        new ClearanceDecisionCheck
                        {
                            CheckCode = "H218",
                            DecisionCode = "C02",
                            DecisionsValidUntil = DateTime.Now,
                            DecisionReasons = ["Some decision reason"],
                        },
                    ],
                },
            ],
        };

        _message = new ResourceEvent<CustomsDeclarationEvent>
        {
            ResourceId = Mrn,
            ResourceType = "CustomsDeclaration",
            SubResourceType = "ClearanceDecision",
            Operation = "Updated",
            Resource = new CustomsDeclarationEvent { Id = "test", ClearanceDecision = clearanceDecision },
        };

        _consumer = new ClearanceDecisionConsumer(
            _decisionSender,
            _logger,
            new OptionsWrapper<CdsOptions>(new CdsOptions { Username = "test-username", Password = "test-password" })
        );
    }

    [Fact]
    public async Task When_processing_succeeds_Then_message_should_be_sent()
    {
        var sendDecisionResult = new RoutingResult
        {
            StatusCode = HttpStatusCode.OK,
            ResponseDate = DateTimeOffset.UtcNow,
        };

        _decisionSender
            .SendDecisionAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<MessagingConstants.MessageSource>(),
                Arg.Any<RoutingResult>(),
                Arg.Any<IHeaderDictionary>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(sendDecisionResult);

        await _consumer.OnHandle(_message, CancellationToken.None);

        await _decisionSender
            .Received(1)
            .SendDecisionAsync(
                Mrn,
                decision: Arg.Is<string>(soap =>
                    soap.Contains(Mrn) && soap.Contains("test-username") && soap.Contains("test-password")
                ),
                MessagingConstants.MessageSource.Btms,
                RoutingResult.Empty,
                headers: null,
                CorrelationId,
                CancellationToken.None
            );
    }

    [Fact]
    public async Task When_resource_is_null_Then_message_exception_is_thrown()
    {
        var message = new ResourceEvent<CustomsDeclarationEvent>
        {
            ResourceId = "24GB123456789AB012",
            ResourceType = "CustomsDeclaration",
            SubResourceType = "ClearanceDecision",
            Operation = "Updated",
            Resource = null,
        };

        var thrownException = await Assert.ThrowsAsync<ClearanceDecisionProcessingException>(() =>
            _consumer.OnHandle(message, CancellationToken.None)
        );
        thrownException.Message.Should().Be("24GB123456789AB012 Failed to process clearance decision resource event.");
        thrownException.InnerException.Should().BeAssignableTo<InvalidOperationException>();
        thrownException
            .InnerException?.Message.Should()
            .Be("24GB123456789AB012 Customs Declaration Resource Event contained a null resource.");
    }

    [Fact]
    public async Task When_customs_declaration_does_not_contain_clearance_decision_Then_exception_is_thrown()
    {
        var message = new ResourceEvent<CustomsDeclarationEvent>
        {
            ResourceId = "24GB123456789AB012",
            ResourceType = "CustomsDeclaration",
            SubResourceType = "ClearanceDecision",
            Operation = "Updated",
            Resource = new CustomsDeclarationEvent() { Id = "test" },
        };

        var thrownException = await Assert.ThrowsAsync<ClearanceDecisionProcessingException>(() =>
            _consumer.OnHandle(message, CancellationToken.None)
        );
        thrownException.Message.Should().Be("24GB123456789AB012 Failed to process clearance decision resource event.");
        thrownException.InnerException.Should().BeAssignableTo<InvalidOperationException>();
        thrownException
            .InnerException?.Message.Should()
            .Be("24GB123456789AB012 Customs Declaration does not contain a Clearance Decision.");
    }

    [Fact]
    public async Task When_processing_fails_Then_exception_is_thrown()
    {
        _decisionSender
            .SendDecisionAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<MessagingConstants.MessageSource>(),
                Arg.Any<RoutingResult>(),
                Arg.Any<IHeaderDictionary>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>()
            )
            .ThrowsAsync(new Exception("Something went wrong"));

        var thrownException = await Assert.ThrowsAsync<ClearanceDecisionProcessingException>(() =>
            _consumer.OnHandle(_message, CancellationToken.None)
        );
        thrownException.Message.Should().Be("24GB123456789AB012 Failed to process clearance decision resource event.");
        thrownException.InnerException.Should().BeAssignableTo<Exception>();
        thrownException.InnerException?.Message.Should().Be("Something went wrong");
    }

    [Fact]
    public async Task When_sending_to_decision_comparer_is_not_successful_Then_exception_is_thrown()
    {
        var sendDecisionResult = new RoutingResult
        {
            StatusCode = HttpStatusCode.BadRequest,
            ResponseDate = DateTimeOffset.UtcNow,
        };

        _decisionSender
            .SendDecisionAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<MessagingConstants.MessageSource>(),
                Arg.Any<RoutingResult>(),
                Arg.Any<IHeaderDictionary>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(sendDecisionResult);

        var thrownException = await Assert.ThrowsAsync<ClearanceDecisionProcessingException>(() =>
            _consumer.OnHandle(_message, CancellationToken.None)
        );
        thrownException.Message.Should().Be("24GB123456789AB012 Failed to process clearance decision resource event.");
        thrownException.InnerException.Should().BeAssignableTo<ClearanceDecisionProcessingException>();
        thrownException.InnerException?.Message.Should().Be("24GB123456789AB012 Failed to send clearance decision.");
    }

    [Fact]
    public async Task When_processing_inbound_error_Then_discarded()
    {
        var message = new ResourceEvent<CustomsDeclarationEvent>
        {
            ResourceId = "24GB123456789AB012",
            ResourceType = "CustomsDeclaration",
            SubResourceType = "InboundError",
            Operation = "Updated",
        };

        await _consumer.OnHandle(message, CancellationToken.None);

        await _decisionSender
            .DidNotReceive()
            .SendDecisionAsync(
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
    public async Task When_sending_to_comparer_returns_conflict_exception_Then_conflict_exception_is_thrown()
    {
        _decisionSender
            .SendDecisionAsync(
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
            _consumer.OnHandle(_message, CancellationToken.None)
        );
        thrownException.Message.Should().Be("24GB123456789AB012 Failed to process clearance decision resource event.");
        thrownException.InnerException.Should().BeAssignableTo<Exception>();
        thrownException.InnerException?.Message.Should().Be("Something went wrong");
    }
}
