using BtmsGateway.Services.Routing;
using FluentAssertions;
using NSubstitute;
using Serilog;

namespace BtmsGateway.Test.Services.Routing;

public class MessageRoutesTests
{
    [Theory]
    [InlineData("/route-1/sub-path/", "http://url-1/sub-path")]
    [InlineData("/route-2/sub-path/", "http://url-2/sub-path")]
    public void When_routing_routed_route_Then_should_get_correct_route(string routedPath, string expectedRoutePath)
    {
        var messageRoutes = new MessageRoutes(TestRoutes.RoutingConfig, Substitute.For<ILogger>());

        var route = messageRoutes.GetRoutedRoute(routedPath);

        route.RouteFound.Should().BeTrue();
        route.RouteUrl.Should().Be(expectedRoutePath);
    }

    [Theory]
    [InlineData("/route-1/sub-path/", "http://url-2/sub-path")]
    [InlineData("/route-2/sub-path/", "http://url-3/sub-path")]
    public void When_routing_forked_route_Then_should_get_correct_route(string routedPath, string expectedRoutePath)
    {
        var messageRoutes = new MessageRoutes(TestRoutes.RoutingConfig, Substitute.For<ILogger>());

        var route = messageRoutes.GetForkedRoute(routedPath);

        route.RouteFound.Should().BeTrue();
        route.RouteUrl.Should().Be(expectedRoutePath);
    }
}