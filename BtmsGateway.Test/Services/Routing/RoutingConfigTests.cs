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
        route.LegacyLink.Should().Be("http://legacy-link-url");
        route.LegacyLinkType.Should().Be(LinkType.Url);
        route.LegacyHostHeader.Should().Be("legacy-host-header");
        route.BtmsLink.Should().Be("btms-link-queue");
        route.BtmsLinkType.Should().Be(LinkType.Queue);
        route.BtmsHostHeader.Should().BeNull();
        route.SendLegacyResponseToBtms.Should().BeTrue();
        route.RouteTo.Should().Be(RouteTo.Legacy);
    }
    
    [Fact]
    public void When_getting_route_2_Then_should_retrieve_routed_links()
    {
        var route = TestRoutes.RoutingConfig.AllRoutes.Single(x => x.Name == "route-2");
        route.Name.Should().Be("route-2");
        route.LegacyLink.Should().Be("legacy-link-queue");
        route.LegacyLinkType.Should().Be(LinkType.Queue);
        route.LegacyHostHeader.Should().BeNull();
        route.BtmsLink.Should().Be("http://btms-link-url");
        route.BtmsLinkType.Should().Be(LinkType.Url);
        route.BtmsHostHeader.Should().Be("btms-host-header");
        route.SendLegacyResponseToBtms.Should().BeTrue();
        route.RouteTo.Should().Be(RouteTo.Btms);
    }
}