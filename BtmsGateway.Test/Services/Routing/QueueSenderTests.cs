using System.Net;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using BtmsGateway.Services.Routing;
using FluentAssertions;
using NSubstitute;
using Serilog;
using IMetrics = BtmsGateway.Utils.IMetrics;

namespace BtmsGateway.Test.Services.Routing;

public class QueueSenderTests
{
    [Fact]
    public async Task SendAsync_WithFork_EncountersError_ReturnsErrorResult()
    {
        // Arrange
        var mocks = CreateMocks(HttpStatusCode.BadRequest);
        var msgData = await TestHelpers.CreateMessageData(mocks.Logger);
        var sut = new QueueSender(mocks.SnsService);
        
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
        var sut = new QueueSender(mocks.SnsService);
        
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
        var sut = new QueueSender(mocks.SnsService);
        
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
        var sut = new QueueSender(mocks.SnsService);
        
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
        var sut = new QueueSender(mocks.SnsService);
        
        // Act
        var response = await sut.Send(msgData.Routing, msgData.Message, mocks.Metrics);
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.RoutingSuccessful.Should().BeTrue();
        mocks.Metrics.DidNotReceive().StartForkedRequest();
        mocks.Metrics.Received().StartRoutedRequest();
    }
    
    private (IAmazonSimpleNotificationService SnsService, ILogger Logger, IMetrics Metrics) CreateMocks(HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        var snsService = Substitute.For<IAmazonSimpleNotificationService>();
        snsService.PublishAsync(Arg.Any<PublishRequest>()).Returns(new PublishResponse()
        {
            HttpStatusCode = statusCode
        });
        
        var logger = Substitute.For<ILogger>();
        var metrics = Substitute.For<IMetrics>();
        
        return (snsService, logger, metrics);
    }
}