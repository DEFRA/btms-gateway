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
        route.MessageBodyDepth.Should().Be(1);
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
        route.MessageBodyDepth.Should().Be(2);
        route.BtmsLink.Should().Be("http://btms-link-url");
        route.BtmsLinkType.Should().Be(LinkType.Url);
        route.BtmsHostHeader.Should().Be("btms-host-header");
        route.SendLegacyResponseToBtms.Should().BeTrue();
        route.RouteTo.Should().Be(RouteTo.Btms);
    }
    
    [Fact]
    public void When_getting_route_3_Then_should_retrieve_routed_links()
    {
        var route = TestRoutes.RoutingConfig.AllRoutes.Single(x => x.Name == "route-3");
        route.Name.Should().Be("route-3");
        route.LegacyLink.Should().Be("http://legacy-link-url");
        route.LegacyLinkType.Should().Be(LinkType.Url);
        route.LegacyHostHeader.Should().Be("legacy-host-header");
        route.MessageBodyDepth.Should().Be(1);
        route.BtmsLink.Should().Be("none");
        route.BtmsLinkType.Should().Be(LinkType.None);
        route.BtmsHostHeader.Should().BeNull();
        route.SendLegacyResponseToBtms.Should().BeFalse();
        route.RouteTo.Should().Be(RouteTo.Legacy);
    }

    [Fact]
    public void When_getting_route_4_Then_should_retrieve_routed_links()
    {
        var route = TestRoutes.RoutingConfig.AllRoutes.Single(x => x.Name == "route-4");
        route.Name.Should().Be("route-4");
        route.LegacyLink.Should().Be("none");
        route.LegacyLinkType.Should().Be(LinkType.None);
        route.LegacyHostHeader.Should().BeNull();
        route.MessageBodyDepth.Should().Be(1);
        route.BtmsLink.Should().Be("http://btms-link-url");
        route.BtmsLinkType.Should().Be(LinkType.Url);
        route.BtmsHostHeader.Should().Be("btms-host-header");
        route.SendLegacyResponseToBtms.Should().BeFalse();
        route.RouteTo.Should().Be(RouteTo.Btms);
    }
}