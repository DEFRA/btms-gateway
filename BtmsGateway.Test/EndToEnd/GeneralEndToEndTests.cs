using System.Net;
using System.Net.Mime;
using System.Text;
using BtmsGateway.Middleware;
using BtmsGateway.Services.Routing;
using BtmsGateway.Test.TestUtils;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace BtmsGateway.Test.EndToEnd;

public sealed class GeneralEndToEndTests : IDisposable
{
    private bool _disposed;

    private const string RoutedPath = "/test/path";

    private readonly string _headerCorrelationId = Guid.NewGuid().ToString("D");
    private readonly string _soapContent;
    private readonly DateTimeOffset _headerDate = DateTimeOffset.UtcNow.AddSeconds(-1).RoundDownToSecond();
    private readonly TestWebServer _testWebServer;
    private readonly HttpClient _httpClient;
    private readonly string _expectedRoutedUrl;
    private readonly string _expectedForkedUrl;
    private readonly StringContent _stringContent;

    public GeneralEndToEndTests()
    {
        _soapContent = $"<Envelope><Body><Message><Xml>Content</Xml><CorrelationId>{_headerCorrelationId}</CorrelationId></Message></Body></Envelope>";
        var routingConfig = new RoutingConfig
        {
            NamedRoutes = new Dictionary<string, NamedRoute>
            {
                {
                    "Test", new NamedRoute
                    {
                        RoutePath = "test/path",
                        LegacyLinkName = "TestPath",
                        BtmsLinkName = "BtmsPath",
                        Legend = "legend",
                        MessageSubXPath = "Message",
                        RouteTo = RouteTo.Legacy
                    }
                }
            },
            NamedLinks = new Dictionary<string, NamedLink>
            {
                { "TestPath", new NamedLink { Link = "http://TestUrlPath", LinkType = LinkType.Url } },
                { "BtmsPath", new NamedLink { Link = "http://BtmsUrlPath/forked", LinkType = LinkType.Url } }
            },
            Destinations = new Dictionary<string, Destination>
            {
                { "destination-1", new Destination { LinkType = LinkType.Url, Link = "http://destination-url", RoutePath = "/route/path-1", ContentType = "application/soap+xml", HostHeader = "syst32.hmrc.gov.uk", Method = "POST" } },
            }
        };
        _testWebServer = TestWebServer.BuildAndRun(ServiceDescriptor.Singleton(routingConfig));
        _httpClient = _testWebServer.HttpServiceClient;
        _httpClient.DefaultRequestHeaders.Date = _headerDate;
        _httpClient.DefaultRequestHeaders.Add(MessageData.CorrelationIdHeaderName, _headerCorrelationId);

        _expectedRoutedUrl = $"http://testurlpath{RoutedPath}";
        _expectedForkedUrl = $"http://btmsurlpath/forked{RoutedPath}";
        _stringContent = new StringContent(_soapContent, Encoding.UTF8, MediaTypeNames.Application.Xml);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            var disposeTask = _testWebServer.DisposeAsync();
            if (disposeTask.IsCompleted)
            {
                return;
            }
            disposeTask.AsTask().GetAwaiter().GetResult();
        }

        _disposed = true;
    }

    [Fact]
    public async Task When_routing_request_Then_should_respond_from_routed_request()
    {
        _testWebServer.RoutedHttpHandler.SetNextResponse(content: _soapContent);

        var response = await _httpClient.PostAsync(RoutedPath, _stringContent);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.ToString().Should().Be(MediaTypeNames.Application.Xml);
        response.Headers.Date.Should().BeAfter(_headerDate);
        response.Headers.GetValues(MessageData.CorrelationIdHeaderName).FirstOrDefault().Should().Be(_headerCorrelationId);
        response.Headers.GetValues(MessageData.RequestedPathHeaderName).FirstOrDefault().Should().Be(RoutedPath);
        (await response.Content.ReadAsStringAsync()).Should().Be(_soapContent);
    }

    [Fact]
    public async Task When_routing_routed_request_Then_should_route_correctly()
    {
        _testWebServer.RoutedHttpHandler.SetNextResponse(content: _soapContent);

        await _httpClient.PostAsync(RoutedPath, _stringContent);

        var request = _testWebServer.RoutedHttpHandler.LastRequest;
        request?.RequestUri?.ToString().Should().Be(_expectedRoutedUrl);
        request?.Method.ToString().Should().Be("POST");
        (await request?.Content?.ReadAsStringAsync()!).Should().Be(_soapContent);
        request?.Content?.Headers.ContentType?.ToString().Should().StartWith(MediaTypeNames.Application.Xml);
        request?.Headers.Date?.Should().Be(_headerDate);
        request?.Headers.GetValues(MessageData.CorrelationIdHeaderName).FirstOrDefault().Should().Be(_headerCorrelationId);

        var response = _testWebServer.RoutedHttpHandler.LastResponse;
        response?.StatusCode.Should().Be(HttpStatusCode.OK);
        response?.Content.Headers.ContentType?.ToString().Should().StartWith(MediaTypeNames.Application.Xml);
        (await response?.Content.ReadAsStringAsync()!).Should().Be(_soapContent);
    }

    [Fact]
    public async Task When_routing_forked_request_Then_should_route_correctly()
    {
        var xmlForkedResponse = "<Envelope><Body><Message><Xml>ForkedResponse</Xml></Message></Body></Envelope>";
        var jsonContent = $"{{{Environment.NewLine}  \"xml\": \"Content\",{Environment.NewLine}  \"correlationId\": \"{_headerCorrelationId}\"{Environment.NewLine}}}";

        _testWebServer.ForkedHttpHandler.SetNextResponse(content: xmlForkedResponse);

        await _httpClient.PostAsync(RoutedPath, _stringContent);

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
        (await response?.Content.ReadAsStringAsync()!).Should().Be(xmlForkedResponse);
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

        var response = await _httpClient.PostAsync(RoutedPath, _stringContent);

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

        var response = await _httpClient.PostAsync(RoutedPath, _stringContent);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task When_routed_request_returns_502_Then_should_retry()
    {
        var callNum = 0;
        _testWebServer.RoutedHttpHandler.SetNextResponse(statusFunc: () => ++callNum == 1 ? HttpStatusCode.BadGateway : HttpStatusCode.OK);

        var response = await _httpClient.PostAsync(RoutedPath, _stringContent);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        callNum.Should().Be(2);
    }

    [Fact]
    public async Task When_forked_request_returns_502_Then_should_retry()
    {
        var callNum = 0;
        _testWebServer.ForkedHttpHandler.SetNextResponse(statusFunc: () => ++callNum == 1 ? HttpStatusCode.BadGateway : HttpStatusCode.OK);

        var response = await _httpClient.PostAsync(RoutedPath, _stringContent);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        callNum.Should().Be(2);
    }
}