using System.Net;
using BtmsGateway.Consumers;
using BtmsGateway.Domain;
using BtmsGateway.Exceptions;
using BtmsGateway.Services.Routing;
using Defra.TradeImportsDataApi.Api.Client;
using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;
using Defra.TradeImportsDataApi.Domain.Events;
using FluentAssertions;
using ILogger = Serilog.ILogger;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NSubstitute.ReturnsExtensions;
using SlimMessageBus;

namespace BtmsGateway.Test.Consumers;

public class ClearanceDecisionConsumerTests
{
    private readonly ITradeImportsDataApiClient _tradeImportsDataApiClient = Substitute.For<ITradeImportsDataApiClient>();
    private readonly IDecisionSender _decisionSender = Substitute.For<IDecisionSender>();
    private readonly ILogger _logger = Substitute.For<ILogger>();
    private readonly IConsumerContext<ResourceEvent<CustomsDeclaration>> _context = Substitute.For<IConsumerContext<ResourceEvent<CustomsDeclaration>>>();
    private ClearanceDecisionConsumer _consumer;

    public ClearanceDecisionConsumerTests()
    {
        var resourceEvent = new ResourceEvent<CustomsDeclaration>
        {
            ResourceId = "24GB123456789AB012",
            ResourceType = "CustomsDeclaration",
            Operation = "Updated"
        };

        _context.Message.Returns(resourceEvent);

        var clearanceDecision = new ClearanceDecision
        {
            ExternalCorrelationId = "external-correlation-id",
            Timestamp = DateTime.Now,
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
                            DecisionReasons =
                            [
                                "Some decision reason"
                            ]
                        }
                    ]
                }
            ]
        };

        var customsDeclaration = new CustomsDeclarationResponse(
            "24GB123456789AB012",
            null,
            clearanceDecision,
            null,
            DateTime.Now,
            DateTime.Now,
            null);

        _tradeImportsDataApiClient.GetCustomsDeclaration(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(customsDeclaration);

        _consumer = new ClearanceDecisionConsumer(_tradeImportsDataApiClient, _decisionSender, _logger);
    }

    [Fact]
    public async Task When_processing_succeeds_Then_message_should_be_sent()
    {
        var sendDecisionResult = new RoutingResult
        {
            StatusCode = HttpStatusCode.OK
        };
        
        _decisionSender.SendDecisionAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<MessagingConstants.DecisionSource>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns(sendDecisionResult);

        await _consumer.OnHandle(_context, CancellationToken.None);

        await _decisionSender.Received(1).SendDecisionAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<MessagingConstants.DecisionSource>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task When_message_is_null_Then_exception_is_thrown()
    {
        _context.Message.ReturnsNull();

        var thrownException = await Assert.ThrowsAsync<InvalidOperationException>(() => _consumer.OnHandle(_context, CancellationToken.None));
        thrownException.Message.Should().StartWith("Invalid message received from queue");
    }

    [Fact]
    public async Task When_api_customs_declaration_is_null_Then_exception_is_thrown()
    {
        _tradeImportsDataApiClient.GetCustomsDeclaration(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ReturnsNull();

        var thrownException = await Assert.ThrowsAsync<ClearanceDecisionProcessingException>(() => _consumer.OnHandle(_context, CancellationToken.None));
        thrownException.Message.Should().Be("24GB123456789AB012 Failed to process clearance decision resource event.");
        thrownException.InnerException.Should().BeAssignableTo<InvalidOperationException>();
        thrownException.InnerException?.Message.Should().Be("24GB123456789AB012 Customs Declaration not found from Data API.");
    }

    [Fact]
    public async Task When_api_customs_declaration_does_not_contain_clearance_decision_Then_exception_is_thrown()
    {
        var customsDeclaration = new CustomsDeclarationResponse(
            "24GB123456789AB012",
            null,
            null,
            null,
            DateTime.Now,
            DateTime.Now,
            null);

        _tradeImportsDataApiClient.GetCustomsDeclaration(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(customsDeclaration);

        var thrownException = await Assert.ThrowsAsync<ClearanceDecisionProcessingException>(() => _consumer.OnHandle(_context, CancellationToken.None));
        thrownException.Message.Should().Be("24GB123456789AB012 Failed to process clearance decision resource event.");
        thrownException.InnerException.Should().BeAssignableTo<InvalidOperationException>();
        thrownException.InnerException?.Message.Should().Be("24GB123456789AB012 Customs Declaration does not contain a Clearance Decision.");
    }

    [Fact]
    public async Task When_processing_fails_Then_exception_is_thrown()
    {
        _decisionSender.SendDecisionAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<MessagingConstants.DecisionSource>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>())
            .ThrowsAsync(new Exception("Something went wrong"));

        var thrownException = await Assert.ThrowsAsync<ClearanceDecisionProcessingException>(() => _consumer.OnHandle(_context, CancellationToken.None));
        thrownException.Message.Should().Be("24GB123456789AB012 Failed to process clearance decision resource event.");
        thrownException.InnerException.Should().BeAssignableTo<Exception>();
        thrownException.InnerException?.Message.Should().Be("Something went wrong");
    }

    [Fact]
    public async Task When_sending_to_decision_comparer_is_not_successful_Then_exception_is_thrown()
    {
        var sendDecisionResult = new RoutingResult
        {
            StatusCode = HttpStatusCode.BadRequest
        };
        
        _decisionSender.SendDecisionAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<MessagingConstants.DecisionSource>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns(sendDecisionResult);

        var thrownException = await Assert.ThrowsAsync<ClearanceDecisionProcessingException>(() => _consumer.OnHandle(_context, CancellationToken.None));
        thrownException.Message.Should().Be("24GB123456789AB012 Failed to process clearance decision resource event.");
        thrownException.InnerException.Should().BeAssignableTo<ClearanceDecisionProcessingException>();
        thrownException.InnerException?.Message.Should().Be("24GB123456789AB012 Failed to send clearance decision to Decision Comparer.");
    }
}