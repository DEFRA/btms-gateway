using System.Net;
using BtmsGateway.Domain;
using BtmsGateway.Middleware;
using BtmsGateway.Services.Converter;
using BtmsGateway.Services.Metrics;
using BtmsGateway.Services.Routing;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Serilog;

namespace BtmsGateway.Test.Services.Routing;

public class MessageRouterTests
{
    [Fact]
    public async Task Fork_NoRouteFound_ShouldReturnRouteError()
    {
        // Arrange
        var mocks = CreateMocks();
        var msgData = await TestHelpers.CreateMessageData(mocks.Logger);

        var sut = new MessageRouter(mocks.Routes, mocks.ApiSender, mocks.QueueSender, mocks.Logger, mocks.DecisionSender);

        // Act
        var response = await sut.Fork(msgData.MessageData, mocks.Metrics);

        // Assert
        response.RouteFound.Should().BeFalse();
        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        response.ErrorMessage.Should().Be("Route not found");
        mocks.Metrics.DidNotReceive().StartRoutedRequest();
        mocks.Metrics.DidNotReceive().RecordRoutedRequest(Arg.Any<RoutingResult>());
        mocks.Metrics.DidNotReceive().StartForkedRequest();
        mocks.Metrics.DidNotReceive().RecordForkedRequest(Arg.Any<RoutingResult>());
    }

    [Fact]
    public async Task Fork_QueueLinkType_SuccessfullyRouted_ReturnsSuccess()
    {
        // Arrange
        var mocks = CreateMocks(new RoutingResult()
        {
            ForkLinkType = LinkType.Queue
        }, false);
        var msgData = await TestHelpers.CreateMessageData(mocks.Logger);

        var sut = new MessageRouter(mocks.Routes, mocks.ApiSender, mocks.QueueSender, mocks.Logger, mocks.DecisionSender);

        // Act
        var response = await sut.Fork(msgData.MessageData, mocks.Metrics);

        // Assert
        response.RouteFound.Should().BeTrue();
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.ErrorMessage.Should().BeNull();
        mocks.Metrics.DidNotReceive().StartRoutedRequest();
        mocks.Metrics.DidNotReceive().RecordRoutedRequest(Arg.Any<RoutingResult>());
        mocks.Metrics.Received().StartForkedRequest();
        mocks.Metrics.Received().RecordForkedRequest(Arg.Any<RoutingResult>());
    }

    [Fact]
    public async Task Fork_QueueLinkType_ThrowsException_ReturnsError()
    {
        // Arrange
        var mocks = CreateMocks(new RoutingResult()
        {
            ForkLinkType = LinkType.Queue
        }, true, false);
        var msgData = await TestHelpers.CreateMessageData(mocks.Logger);

        var sut = new MessageRouter(mocks.Routes, mocks.ApiSender, mocks.QueueSender, mocks.Logger, mocks.DecisionSender);

        // Act
        var response = await sut.Fork(msgData.MessageData, mocks.Metrics);

        // Assert
        response.RouteFound.Should().BeTrue();
        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        response.ErrorMessage.Should().StartWith("Error fork");
        mocks.Metrics.DidNotReceive().StartRoutedRequest();
        mocks.Metrics.DidNotReceive().RecordRoutedRequest(Arg.Any<RoutingResult>());
        mocks.Metrics.Received().StartForkedRequest();
        mocks.Metrics.Received().RecordForkedRequest(Arg.Any<RoutingResult>());
    }

    [Fact]
    public async Task Fork_UrlLinkType_SuccessfullyRouted_ReturnsSuccess()
    {
        // Arrange
        var mocks = CreateMocks(new RoutingResult()
        {
            ForkLinkType = LinkType.Url
        }, true, false);
        var msgData = await TestHelpers.CreateMessageData(mocks.Logger);

        var sut = new MessageRouter(mocks.Routes, mocks.ApiSender, mocks.QueueSender, mocks.Logger, mocks.DecisionSender);

        // Act
        var response = await sut.Fork(msgData.MessageData, mocks.Metrics);

        // Assert
        response.RouteFound.Should().BeTrue();
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.ErrorMessage.Should().BeNull();
        mocks.Metrics.DidNotReceive().StartRoutedRequest();
        mocks.Metrics.DidNotReceive().RecordRoutedRequest(Arg.Any<RoutingResult>());
        mocks.Metrics.Received().StartForkedRequest();
        mocks.Metrics.Received().RecordForkedRequest(Arg.Any<RoutingResult>());
    }

    [Fact]
    public async Task Fork_UrlLinkType_ThrowsException_ReturnsError()
    {
        // Arrange
        var mocks = CreateMocks(new RoutingResult()
        {
            ForkLinkType = LinkType.Url
        }, false);
        var msgData = await TestHelpers.CreateMessageData(mocks.Logger);

        var sut = new MessageRouter(mocks.Routes, mocks.ApiSender, mocks.QueueSender, mocks.Logger, mocks.DecisionSender);

        // Act
        var response = await sut.Fork(msgData.MessageData, mocks.Metrics);

        // Assert
        response.RouteFound.Should().BeTrue();
        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        response.ErrorMessage.Should().StartWith("Error fork");
        mocks.Metrics.DidNotReceive().StartRoutedRequest();
        mocks.Metrics.DidNotReceive().RecordRoutedRequest(Arg.Any<RoutingResult>());
        mocks.Metrics.Received().StartForkedRequest();
        mocks.Metrics.Received().RecordForkedRequest(Arg.Any<RoutingResult>());
    }

    [Fact]
    public async Task Fork_DecisionComparerLinkType_SuccessfullyRouted_ReturnsSuccess()
    {
        // Arrange
        var mocks = CreateMocks(new RoutingResult()
        {
            ForkLinkType = LinkType.DecisionComparer
        });
        var msgData = await TestHelpers.CreateMessageData(mocks.Logger);

        var sut = new MessageRouter(mocks.Routes, mocks.ApiSender, mocks.QueueSender, mocks.Logger, mocks.DecisionSender);

        // Act
        var response = await sut.Fork(msgData.MessageData, mocks.Metrics);

        // Assert
        response.RouteFound.Should().BeTrue();
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.ErrorMessage.Should().BeNull();
        mocks.Metrics.DidNotReceive().StartRoutedRequest();
        mocks.Metrics.DidNotReceive().RecordRoutedRequest(Arg.Any<RoutingResult>());
        mocks.Metrics.Received().StartForkedRequest();
        mocks.Metrics.Received().RecordForkedRequest(Arg.Any<RoutingResult>());
    }

    [Fact]
    public async Task Fork_DecisionComparerLinkType_ThrowsException_ReturnsError()
    {
        // Arrange
        var mocks = CreateMocks(new RoutingResult()
        {
            ForkLinkType = LinkType.DecisionComparer
        }, decisionSenderSuccess: false);
        var msgData = await TestHelpers.CreateMessageData(mocks.Logger);

        var sut = new MessageRouter(mocks.Routes, mocks.ApiSender, mocks.QueueSender, mocks.Logger, mocks.DecisionSender);

        // Act
        var response = await sut.Fork(msgData.MessageData, mocks.Metrics);

        // Assert
        response.RouteFound.Should().BeTrue();
        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        response.ErrorMessage.Should().StartWith("Error fork");
        mocks.Metrics.DidNotReceive().StartRoutedRequest();
        mocks.Metrics.DidNotReceive().RecordRoutedRequest(Arg.Any<RoutingResult>());
        mocks.Metrics.Received().StartForkedRequest();
        mocks.Metrics.Received().RecordForkedRequest(Arg.Any<RoutingResult>());
    }

    [Fact]
    public async Task Route_NoRouteFound_ReturnsInternalServerError()
    {
        // Arrange
        var mocks = CreateMocks();
        var msgData = await TestHelpers.CreateMessageData(mocks.Logger);

        var sut = new MessageRouter(mocks.Routes, mocks.ApiSender, mocks.QueueSender, mocks.Logger, mocks.DecisionSender);

        // Act
        var response = await sut.Route(msgData.MessageData, mocks.Metrics);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        response.ErrorMessage.Should().Be("Route not found");
        mocks.Metrics.DidNotReceive().StartForkedRequest();
        mocks.Metrics.DidNotReceive().RecordRoutedRequest(Arg.Any<RoutingResult>());
        mocks.Metrics.DidNotReceive().StartRoutedRequest();
        mocks.Metrics.DidNotReceive().RecordForkedRequest(Arg.Any<RoutingResult>());
    }

    [Fact]
    public async Task Route_QueueLinkType_SuccessfullyRouted_ReturnsSuccess()
    {
        // Arrange
        var mocks = CreateMocks(new RoutingResult()
        {
            RouteLinkType = LinkType.Queue
        }, false);
        var msgData = await TestHelpers.CreateMessageData(mocks.Logger);

        var sut = new MessageRouter(mocks.Routes, mocks.ApiSender, mocks.QueueSender, mocks.Logger, mocks.DecisionSender);

        // Act
        var response = await sut.Route(msgData.MessageData, mocks.Metrics);

        // Assert
        response.RouteFound.Should().BeTrue();
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.ErrorMessage.Should().BeNull();
        mocks.Metrics.Received().StartRoutedRequest();
        mocks.Metrics.Received().RecordRoutedRequest(Arg.Any<RoutingResult>());
        mocks.Metrics.DidNotReceive().StartForkedRequest();
        mocks.Metrics.DidNotReceive().RecordForkedRequest(Arg.Any<RoutingResult>());
    }

    [Fact]
    public async Task Route_QueueLinkType_ThrowsException_ReturnsError()
    {
        // Arrange
        var mocks = CreateMocks(new RoutingResult()
        {
            RouteLinkType = LinkType.Queue
        }, true, false);
        var msgData = await TestHelpers.CreateMessageData(mocks.Logger);

        var sut = new MessageRouter(mocks.Routes, mocks.ApiSender, mocks.QueueSender, mocks.Logger, mocks.DecisionSender);

        // Act
        var response = await sut.Route(msgData.MessageData, mocks.Metrics);

        // Assert
        response.RouteFound.Should().BeTrue();
        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        response.ErrorMessage.Should().StartWith("Error routing");
        mocks.Metrics.Received().StartRoutedRequest();
        mocks.Metrics.Received().RecordRoutedRequest(Arg.Any<RoutingResult>());
        mocks.Metrics.DidNotReceive().StartForkedRequest();
        mocks.Metrics.DidNotReceive().RecordForkedRequest(Arg.Any<RoutingResult>());
    }

    [Fact]
    public async Task Route_UrlLinkType_SuccessfullyRouted_ReturnsSuccess()
    {
        // Arrange
        var mocks = CreateMocks(new RoutingResult
        {
            RouteLinkType = LinkType.Url
        }, true, false);
        var msgData = await TestHelpers.CreateMessageData(mocks.Logger);

        var sut = new MessageRouter(mocks.Routes, mocks.ApiSender, mocks.QueueSender, mocks.Logger, mocks.DecisionSender);

        // Act
        var response = await sut.Route(msgData.MessageData, mocks.Metrics);

        // Assert
        response.RouteFound.Should().BeTrue();
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.ErrorMessage.Should().BeNull();
        mocks.Metrics.Received().StartRoutedRequest();
        mocks.Metrics.Received().RecordRoutedRequest(Arg.Any<RoutingResult>());
        mocks.Metrics.DidNotReceive().StartForkedRequest();
        mocks.Metrics.DidNotReceive().RecordForkedRequest(Arg.Any<RoutingResult>());
    }

    [Fact]
    public async Task Route_UrlLinkType_ThrowsException_ReturnsError()
    {
        // Arrange
        var mocks = CreateMocks(new RoutingResult()
        {
            RouteLinkType = LinkType.Url
        }, false);
        var msgData = await TestHelpers.CreateMessageData(mocks.Logger);

        var sut = new MessageRouter(mocks.Routes, mocks.ApiSender, mocks.QueueSender, mocks.Logger, mocks.DecisionSender);

        // Act
        var response = await sut.Route(msgData.MessageData, mocks.Metrics);

        // Assert
        response.RouteFound.Should().BeTrue();
        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        response.ErrorMessage.Should().StartWith("Error routing");
        mocks.Metrics.Received().StartRoutedRequest();
        mocks.Metrics.Received().RecordRoutedRequest(Arg.Any<RoutingResult>());
        mocks.Metrics.DidNotReceive().StartForkedRequest();
        mocks.Metrics.DidNotReceive().RecordForkedRequest(Arg.Any<RoutingResult>());
    }

    [Fact]
    public async Task Route_DecisionComparerLinkType_SuccessfullyRouted_ReturnsSuccess()
    {
        // Arrange
        var mocks = CreateMocks(new RoutingResult
        {
            RouteLinkType = LinkType.DecisionComparer
        }, true, false);
        var msgData = await TestHelpers.CreateMessageData(mocks.Logger);

        var sut = new MessageRouter(mocks.Routes, mocks.ApiSender, mocks.QueueSender, mocks.Logger, mocks.DecisionSender);

        // Act
        var response = await sut.Route(msgData.MessageData, mocks.Metrics);

        // Assert
        response.RouteFound.Should().BeTrue();
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.ErrorMessage.Should().BeNull();
        mocks.Metrics.Received().StartRoutedRequest();
        mocks.Metrics.Received().RecordRoutedRequest(Arg.Any<RoutingResult>());
        mocks.Metrics.DidNotReceive().StartForkedRequest();
        mocks.Metrics.DidNotReceive().RecordForkedRequest(Arg.Any<RoutingResult>());
    }

    [Fact]
    public async Task Route_DecisionComparerLinkType_ThrowsException_ReturnsError()
    {
        // Arrange
        var mocks = CreateMocks(new RoutingResult()
        {
            RouteLinkType = LinkType.DecisionComparer
        }, decisionSenderSuccess: false);
        var msgData = await TestHelpers.CreateMessageData(mocks.Logger);

        var sut = new MessageRouter(mocks.Routes, mocks.ApiSender, mocks.QueueSender, mocks.Logger, mocks.DecisionSender);

        // Act
        var response = await sut.Route(msgData.MessageData, mocks.Metrics);

        // Assert
        response.RouteFound.Should().BeTrue();
        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        response.ErrorMessage.Should().StartWith("Error routing");
        mocks.Metrics.Received().StartRoutedRequest();
        mocks.Metrics.Received().RecordRoutedRequest(Arg.Any<RoutingResult>());
        mocks.Metrics.DidNotReceive().StartForkedRequest();
        mocks.Metrics.DidNotReceive().RecordForkedRequest(Arg.Any<RoutingResult>());
    }

    private (IMessageRoutes Routes, IApiSender ApiSender, IQueueSender QueueSender, ILogger Logger, IMetrics Metrics, IDecisionSender DecisionSender)
        CreateMocks(RoutingResult routingResult = null, bool apiSuccess = true, bool queueSuccess = true, bool decisionSenderSuccess = true)
    {
        var routes = Substitute.For<IMessageRoutes>();

        routingResult = routingResult == null ? new RoutingResult()
        {
            RouteFound = false,
            StatusCode = HttpStatusCode.InternalServerError,
            ErrorMessage = "Route not found"
        } : routingResult with
        {
            RouteFound = true,
            FullForkLink = "full-fork-link",
            FullRouteLink = "full-route-link",
        };

        routes.GetRoute(Arg.Any<string>(), Arg.Any<SoapContent>()).Returns(routingResult);

        var apiSender = Substitute.For<IApiSender>();

        if (apiSuccess)
        {
            apiSender.Send(Arg.Any<RoutingResult>(), Arg.Any<MessageData>(), Arg.Any<bool>())
                .Returns(routingResult
                    with
                {
                    RoutingSuccessful = true,
                    ResponseContent = $"API Success!",
                    StatusCode = HttpStatusCode.OK
                });
        }
        else
        {
            apiSender.Send(Arg.Any<RoutingResult>(), Arg.Any<MessageData>(), Arg.Any<bool>())
                .ThrowsAsync<Exception>();
        }

        var queueSender = Substitute.For<IQueueSender>();

        if (queueSuccess)
        {
            queueSender.Send(Arg.Any<RoutingResult>(), Arg.Any<MessageData>(), Arg.Any<string>())
                .Returns(routingResult
                    with
                {
                    RoutingSuccessful = true,
                    ResponseContent = $"Queue Success!",
                    StatusCode = HttpStatusCode.OK
                });
        }
        else
        {
            queueSender.Send(Arg.Any<RoutingResult>(), Arg.Any<MessageData>(), Arg.Any<string>())
                .ThrowsAsync<Exception>();
        }

        var logger = Substitute.For<ILogger>();
        var metrics = Substitute.For<IMetrics>();

        var decisionSender = Substitute.For<IDecisionSender>();

        if (decisionSenderSuccess)
        {
            decisionSender.SendDecisionAsync(
                    Arg.Any<string>(),
                    Arg.Any<string>(),
                    Arg.Any<MessagingConstants.DecisionSource>(),
                    Arg.Any<HeaderDictionary>(),
                    Arg.Any<CancellationToken>())
                .Returns(routingResult
                    with
                {
                    RoutingSuccessful = true,
                    ResponseContent = $"Decision Sender Success!",
                    StatusCode = HttpStatusCode.OK
                });
        }
        else
        {
            decisionSender.SendDecisionAsync(
                    Arg.Any<string>(),
                    Arg.Any<string>(),
                    Arg.Any<MessagingConstants.DecisionSource>(),
                    Arg.Any<HeaderDictionary>(),
                    Arg.Any<CancellationToken>())
                .ThrowsAsync<Exception>();
        }

        return (routes, apiSender, queueSender, logger, metrics, decisionSender);
    }
}