using System.Text.Json;
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
        route.BtmsLink.Should().Be("btms-link-queue");
        route.BtmsLinkType.Should().Be(LinkType.Queue);
        route.SendLegacyResponseToBtms.Should().BeTrue();
        route.RouteTo.Should().Be(RouteTo.Legacy);
        var aaa = JsonSerializer.Serialize(TestRoutes.RoutingConfig);
    }
    
    [Fact]
    public void When_getting_route_2_Then_should_retrieve_routed_links()
    {
        var route = TestRoutes.RoutingConfig.AllRoutes.Single(x => x.Name == "route-2");
        route.Name.Should().Be("route-2");
        route.LegacyLink.Should().Be("legacy-link-queue");
        route.LegacyLinkType.Should().Be(LinkType.Queue);
        route.BtmsLink.Should().Be("http://btms-link-url");
        route.BtmsLinkType.Should().Be(LinkType.Url);
        route.SendLegacyResponseToBtms.Should().BeTrue();
        route.RouteTo.Should().Be(RouteTo.Btms);
    }
}