using System.Diagnostics.Metrics;
using System.Net;
using System.Text;
using BtmsGateway.Config;
using BtmsGateway.Exceptions;
using BtmsGateway.Middleware;
using BtmsGateway.Services.Metrics;
using BtmsGateway.Services.Routing;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace BtmsGateway.Test.Middleware;

public class RoutingInterceptorTests
{
    private const string RequestBody =
        "<Envelope><Body><Root><Data>abc</Data><CorrelationId>correlation-id</CorrelationId></Root></Body></Envelope>";

    private readonly DefaultHttpContext _httpContext = new()
    {
        Request =
        {
            Protocol = "HTTP/1.1",
            Scheme = "http",
            Method = "GET",
            Host = new HostString("localhost", 123),
            Body = new MemoryStream(Encoding.UTF8.GetBytes(RequestBody)),
            Headers =
            {
                new KeyValuePair<string, StringValues>("Content-Length", "999"),
                new KeyValuePair<string, StringValues>("X-Custom", "custom"),
            },
        },
    };

    private readonly IOptions<MessageLoggingOptions> _messageLoggingOptions = Substitute.For<
        IOptions<MessageLoggingOptions>
    >();

    public RoutingInterceptorTests()
    {
        var loggingOptions = new MessageLoggingOptions();
        _messageLoggingOptions.Value.Returns(loggingOptions);
    }

    [Fact]
    public async Task When_invoking_and_exception_is_thrown_Then_exception_is_rethrown()
    {
        var messageRouter = Substitute.For<IMessageRouter>();
        messageRouter
            .Route(Arg.Any<MessageData>(), Arg.Any<IMetrics>())
            .ThrowsAsyncForAnyArgs(new Exception("Test exception"));

        var meter = new Meter("test");
        var meterFactory = Substitute.For<IMeterFactory>();
        meterFactory.Create(null!).ReturnsForAnyArgs(meter);
        var metricsHost = Substitute.For<MetricsHost>(meterFactory);

        var sut = new RoutingInterceptor(
            Substitute.For<RequestDelegate>(),
            messageRouter,
            metricsHost,
            Substitute.For<IRequestMetrics>(),
            NullLogger<RoutingInterceptor>.Instance,
            _messageLoggingOptions,
            Substitute.For<IMessageRoutes>()
        );

        var ex = await Assert.ThrowsAsync<RoutingException>(() => sut.InvokeAsync(_httpContext));
        ex.Message.Should().Be($"There was a routing error: Test exception");
        ex.InnerException.Should().BeAssignableTo<Exception>();
        ex.InnerException?.Message.Should().Be("Test exception");
    }

    [Fact]
    public async Task When_request_message_type_is_identified_by_route_Then_message_received_metric_is_recorded()
    {
        var messageRouter = Substitute.For<IMessageRouter>();
        messageRouter
            .Route(Arg.Any<MessageData>(), Arg.Any<IMetrics>())
            .Returns(
                new RoutingResult
                {
                    RouteFound = true,
                    MessageSubXPath = "KnownMessageType",
                    UrlPath = "/some-known-route-path",
                    Legend = "Known Message Type",
                    RouteLinkType = LinkType.Url,
                    RoutingSuccessful = true,
                    FullRouteLink = "http://localhost/some-known-route-path",
                    StatusCode = HttpStatusCode.OK,
                    ResponseContent = RequestBody,
                },
                new RoutingResult
                {
                    RouteFound = true,
                    MessageSubXPath = "KnownMessageType",
                    UrlPath = "/some-broken-route-path",
                    Legend = "Known Message Type",
                    RouteLinkType = LinkType.Url,
                    RoutingSuccessful = false,
                    FullRouteLink = "http://localhost/some-broken-route-path",
                    StatusCode = HttpStatusCode.ServiceUnavailable,
                    ResponseContent = RequestBody,
                }
            );
        messageRouter
            .Fork(Arg.Any<MessageData>(), Arg.Any<IMetrics>())
            .Returns(
                new RoutingResult
                {
                    RouteFound = true,
                    MessageSubXPath = "KnownMessageType",
                    UrlPath = "/some-known-route-path",
                    Legend = "Known Message Type",
                    RouteLinkType = LinkType.Queue,
                    RoutingSuccessful = true,
                    FullForkLink = "some-topic",
                    StatusCode = HttpStatusCode.OK,
                    ResponseContent = RequestBody,
                }
            );

        var meter = new Meter("test");
        var meterFactory = Substitute.For<IMeterFactory>();
        meterFactory.Create(null!).ReturnsForAnyArgs(meter);
        var metricsHost = Substitute.For<MetricsHost>(meterFactory);

        var requestMetric = Substitute.For<IRequestMetrics>();

        var sut = new RoutingInterceptor(
            Substitute.For<RequestDelegate>(),
            messageRouter,
            metricsHost,
            requestMetric,
            NullLogger<RoutingInterceptor>.Instance,
            _messageLoggingOptions,
            Substitute.For<IMessageRoutes>()
        );

        await sut.InvokeAsync(_httpContext);
        await sut.InvokeAsync(_httpContext);

        requestMetric
            .Received(1)
            .MessageReceived(
                Arg.Is("KnownMessageType"),
                Arg.Is("/some-known-route-path"),
                Arg.Is("Known Message Type"),
                Arg.Is("Routing")
            );
        requestMetric
            .Received(1)
            .MessageReceived(
                Arg.Is("KnownMessageType"),
                Arg.Is("/some-broken-route-path"),
                Arg.Is("Known Message Type"),
                Arg.Is("Routing")
            );
        requestMetric
            .Received(2)
            .MessageReceived(
                Arg.Is("KnownMessageType"),
                Arg.Is("/some-known-route-path"),
                Arg.Is("Known Message Type"),
                Arg.Is("Forking")
            );
    }

    [Fact]
    public async Task When_request_message_type_is_identified_by_route_and_successfully_forwarded_Then_message_successfully_sent_metric_is_recorded()
    {
        var messageRouter = Substitute.For<IMessageRouter>();
        messageRouter
            .Route(Arg.Any<MessageData>(), Arg.Any<IMetrics>())
            .Returns(
                new RoutingResult
                {
                    RouteFound = true,
                    MessageSubXPath = "KnownMessageType",
                    UrlPath = "/some-known-route-path",
                    Legend = "Known Message Type",
                    RouteLinkType = LinkType.Url,
                    RoutingSuccessful = true,
                    FullRouteLink = "http://localhost/some-known-route-path",
                    StatusCode = HttpStatusCode.OK,
                    ResponseContent = RequestBody,
                },
                new RoutingResult
                {
                    RouteFound = true,
                    MessageSubXPath = "KnownMessageType",
                    UrlPath = "/some-broken-route-path",
                    Legend = "Known Message Type",
                    RouteLinkType = LinkType.Url,
                    RoutingSuccessful = false,
                    FullRouteLink = "http://localhost/some-broken-route-path",
                    StatusCode = HttpStatusCode.ServiceUnavailable,
                    ResponseContent = RequestBody,
                }
            );
        messageRouter
            .Fork(Arg.Any<MessageData>(), Arg.Any<IMetrics>())
            .Returns(
                new RoutingResult
                {
                    RouteFound = true,
                    MessageSubXPath = "KnownMessageType",
                    UrlPath = "/some-known-route-path",
                    Legend = "Known Message Type",
                    ForkLinkType = LinkType.Queue,
                    RoutingSuccessful = true,
                    FullForkLink = "some-topic",
                    StatusCode = HttpStatusCode.OK,
                    ResponseContent = RequestBody,
                }
            );

        var meter = new Meter("test");
        var meterFactory = Substitute.For<IMeterFactory>();
        meterFactory.Create(null!).ReturnsForAnyArgs(meter);
        var metricsHost = Substitute.For<MetricsHost>(meterFactory);

        var requestMetric = Substitute.For<IRequestMetrics>();

        var sut = new RoutingInterceptor(
            Substitute.For<RequestDelegate>(),
            messageRouter,
            metricsHost,
            requestMetric,
            NullLogger<RoutingInterceptor>.Instance,
            _messageLoggingOptions,
            Substitute.For<IMessageRoutes>()
        );

        await sut.InvokeAsync(_httpContext);
        await sut.InvokeAsync(_httpContext);

        requestMetric
            .Received(1)
            .MessageSuccessfullySent(
                Arg.Is("KnownMessageType"),
                Arg.Is("/some-known-route-path"),
                Arg.Is("Known Message Type"),
                Arg.Is("Routing")
            );
        requestMetric
            .DidNotReceive()
            .MessageSuccessfullySent(
                Arg.Is("KnownMessageType"),
                Arg.Is("/some-broken-route-path"),
                Arg.Is("Known Message Type"),
                Arg.Is("Routing")
            );
        requestMetric
            .Received(2)
            .MessageSuccessfullySent(
                Arg.Is("KnownMessageType"),
                Arg.Is("/some-known-route-path"),
                Arg.Is("Known Message Type"),
                Arg.Is("Forking")
            );
    }

    [Theory]
    [InlineData(true, HttpStatusCode.BadRequest)]
    [InlineData(false, HttpStatusCode.InternalServerError)]
    public async Task When_invoking_and_invalid_soap_exception_is_thrown_Then_routing_exception_is_thrown(
        bool isCdsRoute,
        HttpStatusCode expectedStatusCode
    )
    {
        const string unparsableBody =
            "<Envelope><Body><Root><Data>&</Data><CorrelationId>correlation-id</CorrelationId></Root></Body></Envelope>";
        var unparsableContext = new DefaultHttpContext
        {
            Request =
            {
                Protocol = "HTTP/1.1",
                Scheme = "http",
                Method = "GET",
                Host = new HostString("localhost", 123),
                Body = new MemoryStream(Encoding.UTF8.GetBytes(unparsableBody)),
                Headers =
                {
                    new KeyValuePair<string, StringValues>("Content-Length", "999"),
                    new KeyValuePair<string, StringValues>("X-Custom", "custom"),
                },
                Path = "/some-route-path",
            },
        };

        var meter = new Meter("test");
        var meterFactory = Substitute.For<IMeterFactory>();
        meterFactory.Create(null!).ReturnsForAnyArgs(meter);
        var metricsHost = Substitute.For<MetricsHost>(meterFactory);

        var messageRoutes = Substitute.For<IMessageRoutes>();
        messageRoutes.IsCdsRoute("/some-route-path").Returns(isCdsRoute);

        var sut = new RoutingInterceptor(
            Substitute.For<RequestDelegate>(),
            Substitute.For<IMessageRouter>(),
            metricsHost,
            Substitute.For<IRequestMetrics>(),
            NullLogger<RoutingInterceptor>.Instance,
            _messageLoggingOptions,
            messageRoutes
        );

        await Assert.ThrowsAsync<RoutingException>(() => sut.InvokeAsync(unparsableContext));
        unparsableContext.Response.StatusCode.Should().Be((int)expectedStatusCode);
    }
}
