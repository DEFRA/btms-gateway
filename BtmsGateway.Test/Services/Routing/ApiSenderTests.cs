using System.Net;
using System.Reflection;
using BtmsGateway.Services.Routing;
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
        var response = await sut.Send(msgData.Routing, msgData.MessageData, fork: true);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.RoutingSuccessful.Should().BeFalse();
    }

    [Fact]
    public async Task SendAsync_WithFork_SendsCorrectly_ReturnsOKResult()
    {
        // Arrange
        var mocks = CreateMocks();
        var msgData = await TestHelpers.CreateMessageData(mocks.Logger);
        var sut = new ApiSender(mocks.Factory);

        // Act
        var response = await sut.Send(msgData.Routing, msgData.MessageData, fork: true);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.RoutingSuccessful.Should().BeTrue();
    }

    [Fact]
    public async Task SendAsync_WithoutFork_EncountersError_ReturnsErrorResult()
    {
        // Arrange
        var mocks = CreateMocks(HttpStatusCode.BadRequest);
        var msgData = await TestHelpers.CreateMessageData(mocks.Logger);
        var sut = new ApiSender(mocks.Factory);

        // Act
        var response = await sut.Send(msgData.Routing, msgData.MessageData, fork: false);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.RoutingSuccessful.Should().BeFalse();
    }

    [Fact]
    public async Task SendAsync_WithoutFork_SendsCorrectly_ReturnsOKResult()
    {
        // Arrange
        var mocks = CreateMocks();
        var msgData = await TestHelpers.CreateMessageData(mocks.Logger);
        var sut = new ApiSender(mocks.Factory);

        // Act
        var response = await sut.Send(msgData.Routing, msgData.MessageData, fork: false);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.RoutingSuccessful.Should().BeTrue();
    }

    [Fact]
    public async Task SendAsync_WithXmlPaylod_SendsCorrectly_ReturnsOKResult()
    {
        // Arrange
        var mocks = CreateMocks();
        var msgData = await TestHelpers.CreateMessageData(mocks.Logger);
        var sut = new ApiSender(mocks.Factory);

        // Act
        var response = await sut.Send(msgData.Routing, msgData.MessageData, fork: false);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.RoutingSuccessful.Should().BeTrue();
    }

    [Fact]
    public async Task SendSoapMessageAsync_SendCorrectly_ReturnsOKResult()
    {
        var mocks = CreateMocks(HttpStatusCode.OK);
        var sut = new ApiSender(mocks.Factory);

        var response = await sut.SendSoapMessageAsync(
            "POST",
            "http://some-url",
            "application/soap+xml",
            "foo.com",
            new Dictionary<string, string> { { "foo", "bar" } },
            "soap message",
            CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private static (HttpClientHandler Handler, IHttpClientFactory Factory, ILogger Logger) CreateMocks(HttpStatusCode statusCode = HttpStatusCode.OK)
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

        return (handler, mockFactory, logger);
    }
}