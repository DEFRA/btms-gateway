using BtmsGateway.Services.Routing;
using FluentAssertions;
using NSubstitute;
using Serilog;

namespace BtmsGateway.Test.Services.Routing;

public class MessageRoutesTests
{
    [Fact]
    public void When_routing_route_1_Then_should_route_correctly()
    {
        var messageRoutes = new MessageRoutes(TestRoutes.RoutingConfig, Substitute.For<ILogger>());

        var route = messageRoutes.GetRoute("/route-1/sub/path/");

        route.RouteFound.Should().BeTrue();
        route.RouteName.Should().Be("route-1");
        route.FullRouteLink.Should().Be("http://legacy-link-url/sub/path");
        route.RouteHostHeader.Should().Be("legacy-host-header");
        route.ConvertedRoutedContentToJson.Should().BeFalse();
        route.FullForkLink.Should().Be("btms-link-queue");
        route.ForkHostHeader.Should().BeNull();
        route.ConvertedForkedContentToJson.Should().BeTrue();
        route.UrlPath.Should().Be("/sub/path");
    }
    
    [Fact]
    public void When_routing_route_2_Then_should_route_correctly()
    {
        var messageRoutes = new MessageRoutes(TestRoutes.RoutingConfig, Substitute.For<ILogger>());

        var route = messageRoutes.GetRoute("/route-2/sub/path/");

        route.RouteFound.Should().BeTrue();
        route.RouteName.Should().Be("route-2");
        route.FullRouteLink.Should().Be("http://btms-link-url/sub/path");
        route.RouteHostHeader.Should().Be("btms-host-header");
        route.ConvertedRoutedContentToJson.Should().BeTrue();
        route.FullForkLink.Should().Be("legacy-link-queue");
        route.ForkHostHeader.Should().BeNull();
        route.ConvertedForkedContentToJson.Should().BeFalse();
        route.UrlPath.Should().Be("/sub/path");
    }
    
    [Fact]
    public void When_routing_unrecognised_route_Then_should_fail()
    {
        var messageRoutes = new MessageRoutes(TestRoutes.RoutingConfig, Substitute.For<ILogger>());

        var route = messageRoutes.GetRoute("/route-3/sub/path/");

        route.RouteFound.Should().BeFalse();
        route.RouteName.Should().Be("route-3");
        route.FullRouteLink.Should().BeNull();
        route.RouteHostHeader.Should().BeNull();
        route.FullForkLink.Should().BeNull();
        route.ForkHostHeader.Should().BeNull();
        route.UrlPath.Should().Be("/sub/path");
    }

    [Fact]
    public void When_routing_with_invalid_url_Then_should_fail()
    {
        var act = () => new MessageRoutes(TestRoutes.InvalidUrlConfig, Substitute.For<ILogger>());

        act.Should().Throw<InvalidDataException>().WithMessage("Invalid URL(s) in config");
    }

    [Fact]
    public void When_routing_with_invalid_route_to_Then_should_fail()
    {
        var act = () => new MessageRoutes(TestRoutes.InvalidRouteToConfig, Substitute.For<ILogger>());

        act.Should().Throw<InvalidDataException>().WithMessage("Invalid Route To in config");
    }

    [Fact]
    public void When_routing_with_invalid_link_type_Then_should_fail()
    {
        var act = () => new MessageRoutes(TestRoutes.InvalidLinkTypeConfig, Substitute.For<ILogger>());

        act.Should().Throw<InvalidDataException>().WithMessage("Invalid Link Type in config");
    }
}