using System.Net;
using BtmsGateway.Middleware;
using BtmsGateway.Services.Converter;
using BtmsGateway.Services.Metrics;
using BtmsGateway.Services.Routing;
using FluentAssertions;
using Microsoft.Extensions.Logging.Testing;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace BtmsGateway.Test.Services.Routing;

public class MessageRouterTests
{
    [Fact]
    public async Task Route_NoRouteFound_ReturnsInternalServerError()
    {
        // Arrange
        var mocks = CreateMocks();
        var msgData = await TestHelpers.CreateMessageData(mocks.Logger);

        var sut = new MessageRouter(
            mocks.Routes,
            mocks.ApiSender,
            mocks.QueueSender,
            mocks.Logger,
            mocks.AlvsIpaffsSuccessProvider
        );

        // Act
        var response = await sut.Route(msgData.MessageData, mocks.Metrics);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        response.ErrorMessage.Should().Be("Route not found");
        mocks.Metrics.DidNotReceive().RecordRoutedRequest(Arg.Any<RoutingResult>());
        mocks.Metrics.DidNotReceive().StartRoutedRequest();
    }

    [Fact]
    public async Task Route_QueueLinkType_SuccessfullyRouted_ReturnsSuccess()
    {
        // Arrange
        var mocks = CreateMocks(new RoutingResult { RouteLinkType = LinkType.Queue }, false);
        var msgData = await TestHelpers.CreateMessageData(mocks.Logger);

        var sut = new MessageRouter(
            mocks.Routes,
            mocks.ApiSender,
            mocks.QueueSender,
            mocks.Logger,
            mocks.AlvsIpaffsSuccessProvider
        );

        // Act
        var response = await sut.Route(msgData.MessageData, mocks.Metrics);

        // Assert
        response.RouteFound.Should().BeTrue();
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.ErrorMessage.Should().BeNull();
        mocks.Metrics.Received().StartRoutedRequest();
        mocks.Metrics.Received().RecordRoutedRequest(Arg.Any<RoutingResult>());
    }

    [Fact]
    public async Task Route_QueueLinkType_ThrowsException_ReturnsError()
    {
        // Arrange
        var mocks = CreateMocks(new RoutingResult { RouteLinkType = LinkType.Queue }, true, false);
        var msgData = await TestHelpers.CreateMessageData(mocks.Logger);

        var sut = new MessageRouter(
            mocks.Routes,
            mocks.ApiSender,
            mocks.QueueSender,
            mocks.Logger,
            mocks.AlvsIpaffsSuccessProvider
        );

        // Act
        var response = await sut.Route(msgData.MessageData, mocks.Metrics);

        // Assert
        response.RouteFound.Should().BeTrue();
        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        response.ErrorMessage.Should().StartWith("Error routing");
        mocks.Metrics.Received().StartRoutedRequest();
        mocks.Metrics.Received().RecordRoutedRequest(Arg.Any<RoutingResult>());
    }

    [Fact]
    public async Task Route_UrlLinkType_SuccessfullyRouted_ReturnsSuccess()
    {
        // Arrange
        var mocks = CreateMocks(new RoutingResult { RouteLinkType = LinkType.Url }, true, false);
        var msgData = await TestHelpers.CreateMessageData(mocks.Logger);

        var sut = new MessageRouter(
            mocks.Routes,
            mocks.ApiSender,
            mocks.QueueSender,
            mocks.Logger,
            mocks.AlvsIpaffsSuccessProvider
        );

        // Act
        var response = await sut.Route(msgData.MessageData, mocks.Metrics);

        // Assert
        response.RouteFound.Should().BeTrue();
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.ErrorMessage.Should().BeNull();
        mocks.Metrics.Received().StartRoutedRequest();
        mocks.Metrics.Received().RecordRoutedRequest(Arg.Any<RoutingResult>());
    }

    [Fact]
    public async Task Route_UrlLinkType_ThrowsException_ReturnsError()
    {
        // Arrange
        var mocks = CreateMocks(new RoutingResult { RouteLinkType = LinkType.Url }, false);
        var msgData = await TestHelpers.CreateMessageData(mocks.Logger);

        var sut = new MessageRouter(
            mocks.Routes,
            mocks.ApiSender,
            mocks.QueueSender,
            mocks.Logger,
            mocks.AlvsIpaffsSuccessProvider
        );

        // Act
        var response = await sut.Route(msgData.MessageData, mocks.Metrics);

        // Assert
        response.RouteFound.Should().BeTrue();
        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        response.ErrorMessage.Should().StartWith("Error routing");
        mocks.Metrics.Received().StartRoutedRequest();
        mocks.Metrics.Received().RecordRoutedRequest(Arg.Any<RoutingResult>());
    }

    [Fact]
    public async Task Route_AlvsIpaffsSuccessLinkType_SuccessfullyRouted_ReturnsSuccess()
    {
        // Arrange
        var mocks = CreateMocks(new RoutingResult { RouteLinkType = LinkType.AlvsIpaffsSuccess }, true, false);
        var msgData = await TestHelpers.CreateMessageData(mocks.Logger);

        var sut = new MessageRouter(
            mocks.Routes,
            mocks.ApiSender,
            mocks.QueueSender,
            mocks.Logger,
            mocks.AlvsIpaffsSuccessProvider
        );

        // Act
        var response = await sut.Route(msgData.MessageData, mocks.Metrics);

        // Assert
        response.RouteFound.Should().BeTrue();
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.ErrorMessage.Should().BeNull();
        mocks.Metrics.Received().StartRoutedRequest();
        mocks.Metrics.Received().RecordRoutedRequest(Arg.Any<RoutingResult>());
    }

    [Fact]
    public async Task Route_AlvsIpaffsSuccessLinkType_ThrowsException_ReturnsError()
    {
        // Arrange
        var mocks = CreateMocks(
            new RoutingResult { RouteLinkType = LinkType.AlvsIpaffsSuccess },
            alvsIpaffsSenderSuccess: false
        );
        var msgData = await TestHelpers.CreateMessageData(mocks.Logger);

        var sut = new MessageRouter(
            mocks.Routes,
            mocks.ApiSender,
            mocks.QueueSender,
            mocks.Logger,
            mocks.AlvsIpaffsSuccessProvider
        );

        // Act
        var response = await sut.Route(msgData.MessageData, mocks.Metrics);

        // Assert
        response.RouteFound.Should().BeTrue();
        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        response.ErrorMessage.Should().StartWith("Error routing");
        mocks.Metrics.Received().StartRoutedRequest();
        mocks.Metrics.Received().RecordRoutedRequest(Arg.Any<RoutingResult>());
    }

    private static (
        IMessageRoutes Routes,
        IApiSender ApiSender,
        IQueueSender QueueSender,
        Microsoft.Extensions.Logging.ILogger<MessageRouter> Logger,
        IMetrics Metrics,
        IAlvsIpaffsSuccessProvider AlvsIpaffsSuccessProvider
    ) CreateMocks(
        RoutingResult routingResult = null,
        bool apiSuccess = true,
        bool queueSuccess = true,
        bool alvsIpaffsSenderSuccess = true
    )
    {
        var routes = Substitute.For<IMessageRoutes>();

        routingResult =
            routingResult == null
                ? new RoutingResult
                {
                    RouteFound = false,
                    StatusCode = HttpStatusCode.InternalServerError,
                    ErrorMessage = "Route not found",
                }
                : routingResult with
                {
                    RouteFound = true,
                    FullRouteLink = "full-route-link",
                };

        routes
            .GetRoute(Arg.Any<string>(), Arg.Any<SoapContent>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(routingResult);

        var apiSender = Substitute.For<IApiSender>();

        if (apiSuccess)
        {
            apiSender
                .Send(Arg.Any<RoutingResult>(), Arg.Any<MessageData>())
                .Returns(
                    routingResult with
                    {
                        RoutingSuccessful = true,
                        ResponseContent = $"API Success!",
                        StatusCode = HttpStatusCode.OK,
                    }
                );
        }
        else
        {
            apiSender.Send(Arg.Any<RoutingResult>(), Arg.Any<MessageData>()).ThrowsAsync<Exception>();
        }

        var queueSender = Substitute.For<IQueueSender>();

        if (queueSuccess)
        {
            queueSender
                .Send(Arg.Any<RoutingResult>(), Arg.Any<MessageData>(), Arg.Any<string>())
                .Returns(
                    routingResult with
                    {
                        RoutingSuccessful = true,
                        ResponseContent = $"Queue Success!",
                        StatusCode = HttpStatusCode.OK,
                    }
                );
        }
        else
        {
            queueSender
                .Send(Arg.Any<RoutingResult>(), Arg.Any<MessageData>(), Arg.Any<string>())
                .ThrowsAsync<Exception>();
        }

        var logger = new FakeLogger<MessageRouter>();
        var metrics = Substitute.For<IMetrics>();

        var alvsIpaffsSuccessProvider = Substitute.For<IAlvsIpaffsSuccessProvider>();

        if (alvsIpaffsSenderSuccess)
        {
            alvsIpaffsSuccessProvider
                .SendIpaffsRequest(Arg.Any<RoutingResult>())
                .Returns(
                    routingResult with
                    {
                        RoutingSuccessful = true,
                        ResponseContent = "ALVS IPAFFS Sender Success",
                        StatusCode = HttpStatusCode.OK,
                    }
                );
        }
        else
        {
            alvsIpaffsSuccessProvider.SendIpaffsRequest(Arg.Any<RoutingResult>()).Throws(new Exception());
        }

        return (routes, apiSender, queueSender, logger, metrics, alvsIpaffsSuccessProvider);
    }
}
