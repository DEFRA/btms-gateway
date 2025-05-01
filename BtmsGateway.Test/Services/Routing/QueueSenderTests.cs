using System.Net;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using BtmsGateway.Services.Routing;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using Serilog;

namespace BtmsGateway.Test.Services.Routing;

public class QueueSenderTests
{
    IConfiguration config = new ConfigurationBuilder()
        .AddInMemoryCollection(new List<KeyValuePair<string, string>>() { new("traceHeader", "trace-header") })
        .Build();

    [Fact]
    public async Task SendAsync_WithFork_EncountersError_ReturnsErrorResult()
    {
        // Arrange
        var mocks = CreateMocks(HttpStatusCode.BadRequest);
        var msgData = await TestHelpers.CreateMessageData(mocks.Logger);
        var sut = new QueueSender(mocks.SnsService, config);

        // Act
        var response = await sut.Send(msgData.Routing, msgData.MessageData, "");

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
        var sut = new QueueSender(mocks.SnsService, config);

        // Act
        var response = await sut.Send(msgData.Routing, msgData.MessageData, "");

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
        var sut = new QueueSender(mocks.SnsService, config);

        // Act
        var response = await sut.Send(msgData.Routing, msgData.MessageData, "");

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
        var sut = new QueueSender(mocks.SnsService, config);

        // Act
        var response = await sut.Send(msgData.Routing, msgData.MessageData, "");

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
        var sut = new QueueSender(mocks.SnsService, config);

        // Act
        var response = await sut.Send(msgData.Routing, msgData.MessageData, "");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.RoutingSuccessful.Should().BeTrue();
    }

    private static (IAmazonSimpleNotificationService SnsService, ILogger Logger) CreateMocks(
        HttpStatusCode statusCode = HttpStatusCode.OK
    )
    {
        var snsService = Substitute.For<IAmazonSimpleNotificationService>();
        snsService.PublishAsync(Arg.Any<PublishRequest>()).Returns(new PublishResponse { HttpStatusCode = statusCode });

        var logger = Substitute.For<ILogger>();

        return (snsService, logger);
    }
}
