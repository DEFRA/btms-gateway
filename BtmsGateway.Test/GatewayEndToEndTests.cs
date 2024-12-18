using System.Net;
using System.Net.Mime;
using System.Text;
using BtmsGateway.Test.Utils;
using BtmsGateway.Services;
using BtmsGateway.Services.Routing;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace BtmsGateway.Test;

public class GatewayEndToEndTests : IAsyncDisposable
{
    private const string XmlContent = "<xml>Content</xml>";
    private const string JsonContent = "{\"xml\":\"Content\"}";
    private const string RouteName = "alvs-ipaffs";
    private const string SubPath = "sub/path";
    private const string FullPath = $"{RouteName}/{SubPath}";
    private const string RoutedPath = $"/{SubPath}";
    private const string ForkedPath = $"/forked/{SubPath}";

    private readonly TestMessageFork _testMessageFork = new();
    private readonly string _headerCorrelationId = Guid.NewGuid().ToString("D");
    private readonly DateTimeOffset _headerDate = DateTimeOffset.UtcNow.AddSeconds(-1).RoundDownToSecond();
    private readonly TestWebServer _testWebServer;
    private readonly HttpClient _httpClient;
    private readonly string _expectedRoutedUrl;
    private readonly string _expectedForkedUrl;
    private readonly TestHttpHandler _httpHandler;
    private readonly StringContent _stringContent;

    public GatewayEndToEndTests()
    {
        _testWebServer = TestWebServer.BuildAndRun(ServiceDescriptor.Singleton<IMessageFork>(_testMessageFork));
        _httpClient = _testWebServer.HttpServiceClient;
        _httpClient.DefaultRequestHeaders.Date = _headerDate;
        _httpClient.DefaultRequestHeaders.Add(MessageData.CorrelationIdHeaderName, _headerCorrelationId);
        _httpHandler = _testWebServer.OutboundTestHttpHandler;
        
        var routingConfig = _testWebServer.Services.GetRequiredService<RoutingConfig>();
        var expectedRoutUrl = routingConfig.AllRoutes.Single(x => x.Name == RouteName).LegacyLink;
        _expectedRoutedUrl = $"{expectedRoutUrl.Trim('/')}/{SubPath}";
        _expectedForkedUrl = $"{expectedRoutUrl.Trim('/')}/forked/{SubPath}";
        _stringContent = new StringContent(XmlContent, Encoding.UTF8, MediaTypeNames.Application.Xml);
    }

    public async ValueTask DisposeAsync() => await _testWebServer.DisposeAsync();

    [Fact, ]
    public async Task When_checking_service_health_Then_should_be_healthy()
    {
        var response = await _httpClient.GetAsync("health");
        
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.ToString().Should().Be(MediaTypeNames.Text.Plain);
        (await response.Content.ReadAsStringAsync()).Should().Be("Healthy");
    }

    [Fact]
    public async Task When_routing_request_Then_should_respond_from_routed_request()
    {
        var response = await _httpClient.PostAsync(FullPath, _stringContent);
        _testMessageFork.HasForked.WaitOne();
        
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.ToString().Should().Be(MediaTypeNames.Application.Xml);
        response.Headers.Date.Should().BeAfter(_headerDate);
        response.Headers.GetValues(MessageData.CorrelationIdHeaderName).FirstOrDefault().Should().Be(_headerCorrelationId);
        response.Headers.GetValues(MessageData.RequestedPathHeaderName).FirstOrDefault().Should().Be(RoutedPath);
        (await response.Content.ReadAsStringAsync()).Should().Be(TestHttpHandler.XmlRoutedResponse);
    }

    [Fact]
    public async Task When_routing_routed_request_Then_should_route_correctly()
    {
        await _httpClient.PostAsync(FullPath, _stringContent);
        _testMessageFork.HasForked.WaitOne();

        var request = _httpHandler.Requests[_expectedRoutedUrl];
        request?.RequestUri?.ToString().Should().Be(_expectedRoutedUrl);
        request?.Method.ToString().Should().Be("POST");
        (await request?.Content?.ReadAsStringAsync()!).Should().Be(XmlContent);
        request?.Content?.Headers.ContentType?.ToString().Should().StartWith(MediaTypeNames.Application.Xml);
        request?.Headers.Date?.Should().Be(_headerDate);
        request?.Headers.GetValues(MessageData.CorrelationIdHeaderName).FirstOrDefault().Should().Be(_headerCorrelationId);

        var response = _httpHandler.Responses[_expectedRoutedUrl];
        response?.StatusCode.Should().Be(HttpStatusCode.OK);
        response?.Headers.Date.Should().BeAfter(_headerDate);
        response?.Headers.GetValues(MessageData.CorrelationIdHeaderName).FirstOrDefault().Should().Be(_headerCorrelationId);
        response?.Headers.GetValues(MessageData.RequestedPathHeaderName).FirstOrDefault().Should().Be(RoutedPath);
        response?.Content.Headers.ContentType?.ToString().Should().StartWith(MediaTypeNames.Application.Xml);
        (await response?.Content.ReadAsStringAsync()!).Should().Be(TestHttpHandler.XmlRoutedResponse);
    }

    [Fact]
    public async Task When_routing_forked_request_Then_should_route_correctly()
    {
        await _httpClient.PostAsync(FullPath, _stringContent);
        _testMessageFork.HasForked.WaitOne();

        var request = _httpHandler.Requests[_expectedForkedUrl];
        request?.RequestUri?.ToString().Should().Be(_expectedForkedUrl);
        request?.Method.ToString().Should().Be("POST");
        (await request?.Content?.ReadAsStringAsync()!).Should().Be(JsonContent);
        request?.Content?.Headers.ContentType?.ToString().Should().StartWith(MediaTypeNames.Application.Json);
        request?.Headers.Date?.Should().Be(_headerDate);
        request?.Headers.GetValues(MessageData.CorrelationIdHeaderName).FirstOrDefault().Should().Be(_headerCorrelationId);

        await Task.Delay(1000);
        var response = _httpHandler.Responses[_expectedForkedUrl];
        response?.StatusCode.Should().Be(HttpStatusCode.OK);
        response?.Headers.Date.Should().BeAfter(_headerDate);
        response?.Headers.GetValues(MessageData.CorrelationIdHeaderName).FirstOrDefault().Should().Be(_headerCorrelationId);
        response?.Headers.GetValues(MessageData.RequestedPathHeaderName).FirstOrDefault().Should().Be(ForkedPath);
        response?.Content.Headers.ContentType?.ToString().Should().StartWith(MediaTypeNames.Application.Json);
        (await response?.Content.ReadAsStringAsync()!).Should().Be(TestHttpHandler.XmlRoutedResponse);
    }

    [Fact]
    public async Task When_routed_request_returns_502_Then_should_retry()
    {
        var callNum = 0;
        _testWebServer.OutboundTestHttpHandler.SetResponseStatusCode(_expectedRoutedUrl, () => ++callNum == 1 ? HttpStatusCode.BadGateway : HttpStatusCode.OK);
        
        var response = await _httpClient.PostAsync(FullPath, _stringContent);
        _testMessageFork.HasForked.WaitOne();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        callNum.Should().Be(2);
    }

    [Fact]
    public async Task When_forked_request_returns_502_Then_should_retry()
    {
        var callNum = 0;
        _testWebServer.OutboundTestHttpHandler.SetResponseStatusCode(_expectedForkedUrl, () => ++callNum == 1 ? HttpStatusCode.BadGateway : HttpStatusCode.OK);
        
        var response = await _httpClient.PostAsync(FullPath, _stringContent);
        _testMessageFork.HasForked.WaitOne();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        callNum.Should().Be(2);
    }
}