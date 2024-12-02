using FluentAssertions;

namespace BtmsGateway.Test.Services.Routing;

public class RoutingConfigTests
{
    [Theory]
    [InlineData("route-1", "http://url-1/")]
    [InlineData("route-2", "http://url-2/")]
    [InlineData("route-3", "http://url-3/")]
    [InlineData("route-4", "http://url-1/")]
    public void When_getting_routed_routes_Then_should_retrieve_returned_urls(string routeName, string url)
    {
        TestRoutes.RoutingConfig.AllRoutedRoutes.Single(x => x.Name == routeName).Url.Should().Be(url);
    }
    
    [Theory]
    [InlineData("route-1", "http://url-2/")]
    [InlineData("route-2", "http://url-3/")]
    [InlineData("route-3", "http://url-4/")]
    [InlineData("route-4", "http://url-3/")]
    public void When_getting_forked_routes_Then_should_retrieve_unreturned_urls(string routeName, string url)
    {
        TestRoutes.RoutingConfig.AllForkedRoutes.Single(x => x.Name == routeName).Url.Should().Be(url);
    }
}