using System.Net;
using BtmsGateway.Middleware;
using BtmsGateway.Services.Routing;
using BtmsGateway.Utils;
using FluentAssertions;
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

        var sut = new MessageRouter(mocks.Routes, mocks.ApiSender, mocks.QueueSender, mocks.Logger);

        // Act
        var response = await sut.Fork(msgData.Message, mocks.Metrics);

        // Assert
        response.RouteFound.Should().BeFalse();
        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        response.ErrorMessage.Should().Be("Route not found");
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

        var sut = new MessageRouter(mocks.Routes, mocks.ApiSender, mocks.QueueSender, mocks.Logger);

        // Act
        var response = await sut.Fork(msgData.Message, mocks.Metrics);

        // Assert
        response.RouteFound.Should().BeTrue();
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.ErrorMessage.Should().BeNull();
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

        var sut = new MessageRouter(mocks.Routes, mocks.ApiSender, mocks.QueueSender, mocks.Logger);

        // Act
        var response = await sut.Fork(msgData.Message, mocks.Metrics);

        // Assert
        response.RouteFound.Should().BeTrue();
        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        response.ErrorMessage.Should().StartWith("Error fork");
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

        var sut = new MessageRouter(mocks.Routes, mocks.ApiSender, mocks.QueueSender, mocks.Logger);

        // Act
        var response = await sut.Fork(msgData.Message, mocks.Metrics);

        // Assert
        response.RouteFound.Should().BeTrue();
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.ErrorMessage.Should().BeNull();
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

        var sut = new MessageRouter(mocks.Routes, mocks.ApiSender, mocks.QueueSender, mocks.Logger);

        // Act
        var response = await sut.Fork(msgData.Message, mocks.Metrics);

        // Assert
        response.RouteFound.Should().BeTrue();
        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        response.ErrorMessage.Should().StartWith("Error fork");
    }

    [Fact]
    public async Task Route_NoRouteFound_ReturnsInternalServerError()
    {
        // Arrange
        var mocks = CreateMocks();
        var msgData = await TestHelpers.CreateMessageData(mocks.Logger);

        var sut = new MessageRouter(mocks.Routes, mocks.ApiSender, mocks.QueueSender, mocks.Logger);

        // Act
        var response = await sut.Route(msgData.Message, mocks.Metrics);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        response.ErrorMessage.Should().Be("Route not found");
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

        var sut = new MessageRouter(mocks.Routes, mocks.ApiSender, mocks.QueueSender, mocks.Logger);

        // Act
        var response = await sut.Route(msgData.Message, mocks.Metrics);

        // Assert
        response.RouteFound.Should().BeTrue();
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.ErrorMessage.Should().BeNull();
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

        var sut = new MessageRouter(mocks.Routes, mocks.ApiSender, mocks.QueueSender, mocks.Logger);

        // Act
        var response = await sut.Route(msgData.Message, mocks.Metrics);

        // Assert
        response.RouteFound.Should().BeTrue();
        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        response.ErrorMessage.Should().StartWith("Error routing");
    }

    [Fact]
    public async Task Route_UrlLinkType_SuccessfullyRouted_ReturnsSuccess()
    {
        // Arrange
        var mocks = CreateMocks(new RoutingResult()
        {
            RouteLinkType = LinkType.Url
        }, true, false);
        var msgData = await TestHelpers.CreateMessageData(mocks.Logger);

        var sut = new MessageRouter(mocks.Routes, mocks.ApiSender, mocks.QueueSender, mocks.Logger);

        // Act
        var response = await sut.Route(msgData.Message, mocks.Metrics);

        // Assert
        response.RouteFound.Should().BeTrue();
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.ErrorMessage.Should().BeNull();
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

        var sut = new MessageRouter(mocks.Routes, mocks.ApiSender, mocks.QueueSender, mocks.Logger);

        // Act
        var response = await sut.Route(msgData.Message, mocks.Metrics);

        // Assert
        response.RouteFound.Should().BeTrue();
        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        response.ErrorMessage.Should().StartWith("Error routing");
    }

    private (IMessageRoutes Routes, IApiSender ApiSender, IQueueSender QueueSender, ILogger Logger, IMetrics Metrics)
        CreateMocks(RoutingResult routingResult = null, bool apiSuccess = true, bool queueSuccess = true)
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

        routes.GetRoute(Arg.Any<string>()).Returns(routingResult);

        var apiSender = Substitute.For<IApiSender>();

        if (apiSuccess)
        {
            apiSender.Send(Arg.Any<RoutingResult>(), Arg.Any<MessageData>(), Arg.Any<IMetrics>(), Arg.Any<bool>())
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
            apiSender.Send(Arg.Any<RoutingResult>(), Arg.Any<MessageData>(), Arg.Any<IMetrics>(), Arg.Any<bool>())
                .ThrowsAsync<Exception>();
        }

        var queueSender = Substitute.For<IQueueSender>();

        if (queueSuccess)
        {
            queueSender.Send(Arg.Any<RoutingResult>(), Arg.Any<MessageData>(), Arg.Any<IMetrics>(), Arg.Any<bool>())
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
            queueSender.Send(Arg.Any<RoutingResult>(), Arg.Any<MessageData>(), Arg.Any<IMetrics>(), Arg.Any<bool>())
                .ThrowsAsync<Exception>();
        }

        var logger = Substitute.For<ILogger>();
        var metrics = Substitute.For<IMetrics>();

        return (routes, apiSender, queueSender, logger, metrics);
    }
}