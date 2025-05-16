using System.Net;
using System.Reflection;
using System.Text.Json;
using BtmsGateway.Services.Routing;
using FluentAssertions;
using Microsoft.AspNetCore.HeaderPropagation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
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
        var sut = new ApiSender(mocks.Factory, mocks.ServiceProvider, mocks.Configuration);

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
        var sut = new ApiSender(mocks.Factory, mocks.ServiceProvider, mocks.Configuration);

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
        var sut = new ApiSender(mocks.Factory, mocks.ServiceProvider, mocks.Configuration);

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
        var sut = new ApiSender(mocks.Factory, mocks.ServiceProvider, mocks.Configuration);

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
        var sut = new ApiSender(mocks.Factory, mocks.ServiceProvider, mocks.Configuration);

        // Act
        var response = await sut.Send(msgData.Routing, msgData.MessageData, fork: false);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.RoutingSuccessful.Should().BeTrue();
    }

    [Fact]
    public async Task SendSoapMessageAsync_SendCorrectly_ReturnsOKResult()
    {
        var mocks = CreateMocks();
        var sut = new ApiSender(mocks.Factory, mocks.ServiceProvider, mocks.Configuration);

        var response = await sut.SendSoapMessageAsync(
            "POST",
            "http://some-url",
            "application/soap+xml",
            "foo.com",
            new Dictionary<string, string> { { "foo", "bar" } },
            "soap message",
            CancellationToken.None
        );

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task When_send_decision_Then_should_return_response()
    {
        var mocks = CreateMocks();
        var sut = new ApiSender(mocks.Factory, mocks.ServiceProvider, mocks.Configuration);

        var response = await sut.SendDecisionAsync(
            "<decision />",
            "http://trade-imports-decision-comparer-host",
            "application/soap+xml",
            cancellationToken: CancellationToken.None,
            new HeaderDictionary { new KeyValuePair<string, StringValues>("x-cdp-request-id", "some-request-id") }
        );

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task When_get_ecs_metadata_Then_should_return_response()
    {
        Environment.SetEnvironmentVariable("ECS_CONTAINER_METADATA_URI_V4", "http://test-url");

        var json = await File.ReadAllTextAsync(
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Services", "Routing", "Fixtures", "EcsMetadata.json")
        );
        var content = new StringContent(json);

        var mocks = CreateMocks(content: content);
        var sut = new ApiSender(mocks.Factory, mocks.ServiceProvider, mocks.Configuration);

        var response = await sut.GetEcsMetadataAsync(CancellationToken.None);

        response.Should().NotBeNull();
        response
            ?.TaskArn.Should()
            .Be("arn:aws:ecs:eu-west-2:000000000000:task/dev-ecs-protected/f80c7eb5655547849cda89e80aa0eef8");
    }

    [Fact]
    public async Task When_get_ecs_metadata_and_metadata_uri_is_not_present_Then_should_return_null()
    {
        Environment.SetEnvironmentVariable("ECS_CONTAINER_METADATA_URI_V4", null);

        var mocks = CreateMocks();
        var sut = new ApiSender(mocks.Factory, mocks.ServiceProvider, mocks.Configuration);

        var response = await sut.GetEcsMetadataAsync(CancellationToken.None);

        response.Should().BeNull();
    }

    [Fact]
    public async Task When_get_ecs_metadata_and_metadata_uri_returns_null_Then_should_throw()
    {
        Environment.SetEnvironmentVariable("ECS_CONTAINER_METADATA_URI_V4", "http://test-url");

        var mocks = CreateMocks();
        var sut = new ApiSender(mocks.Factory, mocks.ServiceProvider, mocks.Configuration);

        await Assert.ThrowsAsync<JsonException>(() => sut.GetEcsMetadataAsync(CancellationToken.None));
    }

    private static (
        HttpClientHandler Handler,
        IHttpClientFactory Factory,
        ILogger Logger,
        IServiceProvider ServiceProvider,
        IConfiguration Configuration,
        HeaderPropagationValues HeaderPropagationValues
    ) CreateMocks(HttpStatusCode statusCode = HttpStatusCode.OK, HttpContent content = null)
    {
        var response = new HttpResponseMessage(statusCode);
        if (content is not null)
            response.Content = content;

        var handler = Substitute.ForPartsOf<HttpClientHandler>();
        handler
            .GetType()
            .GetMethod("SendAsync", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(handler, [Arg.Any<HttpRequestMessage>(), Arg.Any<CancellationToken>()])
            .Returns(Task.FromResult(response));

        var mockClient = new HttpClient(handler);
        var mockFactory = Substitute.For<IHttpClientFactory>();
        mockFactory.CreateClient(Arg.Any<string>()).Returns(mockClient);

        var logger = Substitute.For<ILogger>();

        var headerPropagationValues = new HeaderPropagationValues();

        var serviceScope = Substitute.For<IServiceScope>();
        var serviceProvider = Substitute.For<IServiceProvider>();
        serviceProvider.GetService(typeof(HeaderPropagationValues)).Returns(headerPropagationValues);
        var serviceScopeFactory = Substitute.For<IServiceScopeFactory>();
        serviceScopeFactory.CreateScope().Returns(serviceScope);
        serviceProvider.GetService(typeof(IServiceScopeFactory)).Returns(serviceScopeFactory);
        serviceScope.ServiceProvider.Returns(serviceProvider);

        var configSection = Substitute.For<IConfigurationSection>();
        configSection.Value.Returns("x-cdp-request-id");
        var configuration = Substitute.For<IConfiguration>();
        configuration.GetSection("TraceHeader").Returns(configSection);

        return (handler, mockFactory, logger, serviceProvider, configuration, headerPropagationValues);
    }
}
