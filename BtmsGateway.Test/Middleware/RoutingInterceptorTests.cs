using System.Diagnostics.Metrics;
using System.Text;
using BtmsGateway.Exceptions;
using BtmsGateway.Middleware;
using BtmsGateway.Services.Metrics;
using BtmsGateway.Services.Routing;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Serilog;

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
            Substitute.For<ILogger>()
        );

        var ex = await Assert.ThrowsAsync<RoutingException>(() => sut.InvokeAsync(_httpContext));
        ex.Message.Should().Be($"There was a routing error: Test exception");
        ex.InnerException.Should().BeAssignableTo<Exception>();
        ex.InnerException?.Message.Should().Be("Test exception");
    }
}
