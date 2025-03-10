using System.Net;
using System.Reflection;
using BtmsGateway.Services.Routing;
using BtmsGateway.Utils;
using FluentAssertions;
using NSubstitute;
using Serilog;

namespace BtmsGateway.Test.Services.Routing;

public class ApiSenderTests
{
    [Fact]
    public async Task SendAsync_WithFork_EncountersError_ReturnsErrorResult()
    {
        // Arrange
        var mocks = CreateMocks(HttpStatusCode.BadRequest);
        var msgData = await TestHelpers.CreateMessageData(mocks.Logger);
        var sut = new ApiSender(mocks.Factory);

        // Act
        var response = await sut.Send(msgData.Routing, msgData.Message, mocks.Metrics, true);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.RoutingSuccessful.Should().BeFalse();
        mocks.Metrics.Received().StartForkedRequest();
        mocks.Metrics.DidNotReceive().StartRoutedRequest();
    }

    [Fact]
    public async Task SendAsync_WithFork_SendsCorrectly_ReturnsOKResult()
    {
        // Arrange
        var mocks = CreateMocks();
        var msgData = await TestHelpers.CreateMessageData(mocks.Logger);
        var sut = new ApiSender(mocks.Factory);

        // Act
        var response = await sut.Send(msgData.Routing, msgData.Message, mocks.Metrics, true);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.RoutingSuccessful.Should().BeTrue();
        mocks.Metrics.Received().StartForkedRequest();
        mocks.Metrics.DidNotReceive().StartRoutedRequest();
    }

    [Fact]
    public async Task SendAsync_WithoutFork_EncountersError_ReturnsErrorResult()
    {
        // Arrange
        var mocks = CreateMocks(HttpStatusCode.BadRequest);
        var msgData = await TestHelpers.CreateMessageData(mocks.Logger);
        var sut = new ApiSender(mocks.Factory);

        // Act
        var response = await sut.Send(msgData.Routing, msgData.Message, mocks.Metrics);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.RoutingSuccessful.Should().BeFalse();
        mocks.Metrics.DidNotReceive().StartForkedRequest();
        mocks.Metrics.Received().StartRoutedRequest();
    }

    [Fact]
    public async Task SendAsync_WithoutFork_SendsCorrectly_ReturnsOKResult()
    {
        // Arrange
        var mocks = CreateMocks();
        var msgData = await TestHelpers.CreateMessageData(mocks.Logger);
        var sut = new ApiSender(mocks.Factory);

        // Act
        var response = await sut.Send(msgData.Routing, msgData.Message, mocks.Metrics);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.RoutingSuccessful.Should().BeTrue();
        mocks.Metrics.DidNotReceive().StartForkedRequest();
        mocks.Metrics.Received().StartRoutedRequest();
    }

    [Fact]
    public async Task SendAsync_WithXmlPaylod_SendsCorrectly_ReturnsOKResult()
    {
        // Arrange
        var mocks = CreateMocks();
        var msgData = await TestHelpers.CreateMessageData(mocks.Logger, false);
        var sut = new ApiSender(mocks.Factory);

        // Act
        var response = await sut.Send(msgData.Routing, msgData.Message, mocks.Metrics);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.RoutingSuccessful.Should().BeTrue();
        mocks.Metrics.DidNotReceive().StartForkedRequest();
        mocks.Metrics.Received().StartRoutedRequest();
    }

    private (HttpClientHandler Handler, IHttpClientFactory Factory, ILogger Logger, IMetrics Metrics) CreateMocks(HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        var response = new HttpResponseMessage(statusCode);

        var handler = Substitute.ForPartsOf<HttpClientHandler>();
        handler.GetType().GetMethod("SendAsync", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(
                handler,
                [Arg.Any<HttpRequestMessage>(), Arg.Any<CancellationToken>()])
            .Returns(Task.FromResult(response));

        var mockClient = new HttpClient(handler);
        var mockFactory = Substitute.For<IHttpClientFactory>();
        mockFactory.CreateClient(Arg.Any<string>()).Returns(mockClient);

        var logger = Substitute.For<ILogger>();
        var metrics = Substitute.For<IMetrics>();

        return (handler, mockFactory, logger, metrics);
    }
}