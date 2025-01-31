using System.Net;
using System.Net.Mime;
using System.Text;
using BtmsGateway.Middleware;
using BtmsGateway.Services.Routing;
using BtmsGateway.Test.TestUtils;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace BtmsGateway.Test.EndToEnd;

public sealed class GeneralEndToEndTests : IAsyncDisposable
{
    private const string XmlRoutedResponse = "<root><xml>RoutedResponse</xml></root>";
    private const string XmlContent = "<Envelope><Body><Message><xml>Content</xml></Message></Body></Envelope>";
    private const string RouteName = "test";
    private const string SubPath = "sub/path";
    private const string FullPath = $"{RouteName}/{SubPath}";
    private const string RoutedPath = $"/{SubPath}";

    private readonly string _headerCorrelationId = Guid.NewGuid().ToString("D");
    private readonly DateTimeOffset _headerDate = DateTimeOffset.UtcNow.AddSeconds(-1).RoundDownToSecond();
    private readonly TestWebServer _testWebServer;
    private readonly HttpClient _httpClient;
    private readonly string _expectedRoutedUrl;
    private readonly string _expectedForkedUrl;
    private readonly StringContent _stringContent;

    public GeneralEndToEndTests()
    {
        _testWebServer = TestWebServer.BuildAndRun();
        _httpClient = _testWebServer.HttpServiceClient;
        _httpClient.DefaultRequestHeaders.Date = _headerDate;
        _httpClient.DefaultRequestHeaders.Add(MessageData.CorrelationIdHeaderName, _headerCorrelationId);

        var routingConfig = _testWebServer.Services.GetRequiredService<RoutingConfig>();
        var expectedRoutUrl = routingConfig.AllRoutes.Single(x => x.Name == RouteName).LegacyLink!;
        _expectedRoutedUrl = $"{expectedRoutUrl.Trim('/')}/{SubPath}";
        _expectedForkedUrl = $"{expectedRoutUrl.Trim('/')}/forked/{SubPath}";
        _stringContent = new StringContent(XmlContent, Encoding.UTF8, MediaTypeNames.Application.Xml);
    }

    public async ValueTask DisposeAsync() => await _testWebServer.DisposeAsync();

    [Fact]
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
        _testWebServer.RoutedHttpHandler.SetNextResponse(content: XmlRoutedResponse);

        var response = await _httpClient.PostAsync(FullPath, _stringContent);
        
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.ToString().Should().Be(MediaTypeNames.Application.Xml);
        response.Headers.Date.Should().BeAfter(_headerDate);
        response.Headers.GetValues(MessageData.CorrelationIdHeaderName).FirstOrDefault().Should().Be(_headerCorrelationId);
        response.Headers.GetValues(MessageData.RequestedPathHeaderName).FirstOrDefault().Should().Be(RoutedPath);
        (await response.Content.ReadAsStringAsync()).Should().Be(XmlRoutedResponse);
    }

    [Fact]
    public async Task When_routing_routed_request_Then_should_route_correctly()
    {
        _testWebServer.RoutedHttpHandler.SetNextResponse(content: XmlRoutedResponse);

        await _httpClient.PostAsync(FullPath, _stringContent);

        var request = _testWebServer.RoutedHttpHandler.LastRequest;
        request?.RequestUri?.ToString().Should().Be(_expectedRoutedUrl);
        request?.Method.ToString().Should().Be("POST");
        (await request?.Content?.ReadAsStringAsync()!).Should().Be(XmlContent);
        request?.Content?.Headers.ContentType?.ToString().Should().StartWith(MediaTypeNames.Application.Xml);
        request?.Headers.Date?.Should().Be(_headerDate);
        request?.Headers.GetValues(MessageData.CorrelationIdHeaderName).FirstOrDefault().Should().Be(_headerCorrelationId);

        var response = _testWebServer.RoutedHttpHandler.LastResponse;
        response?.StatusCode.Should().Be(HttpStatusCode.OK);
        response?.Content.Headers.ContentType?.ToString().Should().StartWith(MediaTypeNames.Application.Xml);
        (await response?.Content.ReadAsStringAsync()!).Should().Be(XmlRoutedResponse);
    }

    [Fact]
    public async Task When_routing_forked_request_Then_should_route_correctly()
    { 
        const string XmlForkedResponse = "<Envelope><Body><Message><xml>ForkedResponse</xml></Message></Body></Envelope>";
        var jsonContent = $"{{{Environment.NewLine}  \"xml\": \"Content\"{Environment.NewLine}}}";

        _testWebServer.ForkedHttpHandler.SetNextResponse(content: XmlForkedResponse);

        await _httpClient.PostAsync(FullPath, _stringContent);

        var request = _testWebServer.ForkedHttpHandler.LastRequest;
        request?.RequestUri?.ToString().Should().Be(_expectedForkedUrl);
        request?.Method.ToString().Should().Be("POST");
        (await request?.Content?.ReadAsStringAsync()!).Should().Be(jsonContent);
        request?.Content?.Headers.ContentType?.ToString().Should().StartWith(MediaTypeNames.Application.Json);
        request?.Headers.Date?.Should().Be(_headerDate);
        request?.Headers.GetValues(MessageData.CorrelationIdHeaderName).FirstOrDefault().Should().Be(_headerCorrelationId);

        var response = _testWebServer.ForkedHttpHandler.LastResponse;
        response?.StatusCode.Should().Be(HttpStatusCode.OK);
        response?.Content.Headers.ContentType?.ToString().Should().StartWith(MediaTypeNames.Application.Json);
        (await response?.Content.ReadAsStringAsync()!).Should().Be(XmlForkedResponse);
    }

    [Theory]
    [InlineData(HttpStatusCode.OK)]
    [InlineData(HttpStatusCode.Accepted)]
    [InlineData(HttpStatusCode.NoContent)]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.BadRequest)]
    public async Task When_routed_request_returns_specific_status_code_Then_should_respond_with_same_status_code(HttpStatusCode targetStatusCode)
    {
        _testWebServer.RoutedHttpHandler.SetNextResponse(statusFunc: () => targetStatusCode);
        
        var response = await _httpClient.PostAsync(FullPath, _stringContent);

        response.StatusCode.Should().Be(targetStatusCode);
    }

    [Theory]
    [InlineData(HttpStatusCode.OK)]
    [InlineData(HttpStatusCode.Accepted)]
    [InlineData(HttpStatusCode.NoContent)]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.BadRequest)]
    public async Task When_forked_request_returns_specific_status_code_Then_should_respond_with_routed_status_code(HttpStatusCode targetStatusCode)
    {
        _testWebServer.ForkedHttpHandler.SetNextResponse(statusFunc: () => targetStatusCode);
        
        var response = await _httpClient.PostAsync(FullPath, _stringContent);
        
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task When_routed_request_returns_502_Then_should_retry()
    {
        var callNum = 0;
        _testWebServer.RoutedHttpHandler.SetNextResponse(statusFunc: () => ++callNum == 1 ? HttpStatusCode.BadGateway : HttpStatusCode.OK);
        
        var response = await _httpClient.PostAsync(FullPath, _stringContent);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        callNum.Should().Be(2);
    }

    [Fact]
    public async Task When_forked_request_returns_502_Then_should_retry()
    {
        var callNum = 0;
        _testWebServer.ForkedHttpHandler.SetNextResponse(statusFunc: () => ++callNum == 1 ? HttpStatusCode.BadGateway : HttpStatusCode.OK);
        
        var response = await _httpClient.PostAsync(FullPath, _stringContent);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        callNum.Should().Be(2);
    }
}