using BtmsGateway.Services.Routing;
using FluentAssertions;

namespace BtmsGateway.Test.Services.Routing;

public class RoutingConfigTests
{
    [Fact]
    public void When_getting_route_1_Then_should_retrieve_routed_links()
    {
        var route = TestRoutes.RoutingConfig.AllRoutes.Single(x => x.Name == "route-1");
        route.Name.Should().Be("route-1");
        route.Legend.Should().Be("Route 1");
        route.RoutePath.Should().Be("route/path-1/sub/path");
        route.MessageSubXPath.Should().Be("Message1");
        route.LegacyLink.Should().Be("http://legacy-link-url");
        route.LegacyLinkType.Should().Be(LinkType.Url);
        route.LegacyHostHeader.Should().Be("legacy-host-header");
        route.BtmsLink.Should().Be("btms-link-queue");
        route.BtmsLinkType.Should().Be(LinkType.Queue);
        route.BtmsHostHeader.Should().BeNull();
        route.RouteTo.Should().Be(RouteTo.Legacy);
    }

    [Fact]
    public void When_getting_route_2_Then_should_retrieve_routed_links()
    {
        var route = TestRoutes.RoutingConfig.AllRoutes.Single(x => x.Name == "route-2");
        route.Name.Should().Be("route-2");
        route.Legend.Should().Be("Route 2");
        route.RoutePath.Should().Be("route/path-2/sub/path");
        route.MessageSubXPath.Should().Be("Message2");
        route.LegacyLink.Should().Be("legacy-link-queue");
        route.LegacyLinkType.Should().Be(LinkType.Queue);
        route.LegacyHostHeader.Should().BeNull();
        route.BtmsLink.Should().Be("http://btms-link-url");
        route.BtmsLinkType.Should().Be(LinkType.Url);
        route.BtmsHostHeader.Should().Be("btms-host-header");
        route.RouteTo.Should().Be(RouteTo.Btms);
    }

    [Fact]
    public void When_getting_route_3_Then_should_retrieve_routed_links()
    {
        var route = TestRoutes.RoutingConfig.AllRoutes.Single(x => x.Name == "route-3");
        route.Name.Should().Be("route-3");
        route.Legend.Should().Be("Route 3");
        route.RoutePath.Should().Be("route/path-3/sub/path");
        route.MessageSubXPath.Should().Be("Message3");
        route.LegacyLink.Should().Be("http://legacy-link-url");
        route.LegacyLinkType.Should().Be(LinkType.Url);
        route.LegacyHostHeader.Should().Be("legacy-host-header");
        route.BtmsLink.Should().Be("none");
        route.BtmsLinkType.Should().Be(LinkType.None);
        route.BtmsHostHeader.Should().BeNull();
        route.RouteTo.Should().Be(RouteTo.Legacy);
    }

    [Fact]
    public void When_getting_route_4_Then_should_retrieve_routed_links()
    {
        var route = TestRoutes.RoutingConfig.AllRoutes.Single(x => x.Name == "route-4");
        route.Name.Should().Be("route-4");
        route.Legend.Should().Be("Route 4");
        route.RoutePath.Should().Be("route/path-4/sub/path");
        route.MessageSubXPath.Should().Be("Message4");
        route.LegacyLink.Should().Be("none");
        route.LegacyLinkType.Should().Be(LinkType.None);
        route.LegacyHostHeader.Should().BeNull();
        route.BtmsLink.Should().Be("http://btms-link-url");
        route.BtmsLinkType.Should().Be(LinkType.Url);
        route.BtmsHostHeader.Should().Be("btms-host-header");
        route.RouteTo.Should().Be(RouteTo.Btms);
    }
}