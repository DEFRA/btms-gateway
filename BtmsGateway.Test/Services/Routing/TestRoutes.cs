using BtmsGateway.Services.Routing;

namespace BtmsGateway.Test.Services.Routing;

public static class TestRoutes
{
    public static readonly RoutingConfig RoutingConfig = new()
    {
        Routes =
        [
            new NamedRoute { Name = "route-1", RoutedUrlName = "url-1", ForkedUrlName = "url-2" },
            new NamedRoute { Name = "route-2", RoutedUrlName = "url-2", ForkedUrlName = "url-3" }
        ],
        NamedUrls =
        [
            new NamedUrl { Name = "url-1", Url = "http://url-1/" },
            new NamedUrl { Name = "url-2", Url = "http://url-2/" },
            new NamedUrl { Name = "url-3", Url = "http://url-3/" },
            new NamedUrl { Name = "url-4", Url = "http://url-4/" },
        ],
        HealthUrls = []
    };
}