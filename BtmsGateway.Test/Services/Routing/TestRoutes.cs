using BtmsGateway.Services.Routing;

namespace BtmsGateway.Test.Services.Routing;

public static class TestRoutes
{
    public static readonly RoutingConfig RoutingConfig = new()
    {
        NamedRoutes = new List<KeyValuePair<string, NamedRoute>>
        {
            new ("route-1", new NamedRoute { LegacyLinkName = "legacy-link-name-1", BtmsLinkName = "btms-link-name-1", SendRoutedResponseToFork = true, RouteTo = RouteTo.Legacy }),
            new ("route-2", new NamedRoute { LegacyLinkName = "legacy-link-name-2", BtmsLinkName = "btms-link-name-2", SendRoutedResponseToFork = true, RouteTo = RouteTo.Btms })
        },
        NamedLinks = new List<KeyValuePair<string, NamedLink>>
        {
            new ("legacy-link-name-1", new NamedLink { Link = "http://legacy-link-url", LinkType = LinkType.Url }),
            new ("legacy-link-name-2", new NamedLink { Link = "legacy-link-queue", LinkType = LinkType.Queue }),
            new ("btms-link-name-1", new NamedLink { Link = "btms-link-queue", LinkType = LinkType.Queue }),
            new ("btms-link-name-2", new NamedLink { Link = "http://btms-link-url", LinkType = LinkType.Url }),
        }
    };
    
    public static readonly RoutingConfig DuplicateRoutesConfig = new()
    {
        NamedRoutes = new List<KeyValuePair<string, NamedRoute>>
        {
            new("route-1", new NamedRoute { LegacyLinkName = "link-name-1", BtmsLinkName = "link-name-1", SendRoutedResponseToFork = true, RouteTo = RouteTo.Legacy }),
            new("route-1", new NamedRoute { LegacyLinkName = "link-name-1", BtmsLinkName = "link-name-1", SendRoutedResponseToFork = true, RouteTo = RouteTo.Btms })
        },
        NamedLinks = new List<KeyValuePair<string, NamedLink>>
        {
            new("link-name-1", new NamedLink { Link = "http://link-url", LinkType = LinkType.Url } )
        }
    };
    
    public static readonly RoutingConfig DuplicateLinksConfig = new()
    {
        NamedRoutes = new List<KeyValuePair<string, NamedRoute>>
        {
            new ("route-1", new NamedRoute { LegacyLinkName = "link-name-1", BtmsLinkName = "link-name-1", SendRoutedResponseToFork = true, RouteTo = RouteTo.Legacy })
        },
        NamedLinks = new List<KeyValuePair<string, NamedLink>>
        {
            new ("link-name-1", new NamedLink { Link = "http://link-url", LinkType = LinkType.Url }),
            new ("link-name-1", new NamedLink { Link = "http://link-url", LinkType = LinkType.Url })
        }
    };
    
    public static readonly RoutingConfig InvalidUrlConfig = new()
    {
        NamedRoutes = new List<KeyValuePair<string, NamedRoute>>
        {
            new ("route-1", new NamedRoute { LegacyLinkName = "link-name-1", BtmsLinkName = "link-name-1", SendRoutedResponseToFork = true, RouteTo = RouteTo.Legacy })
        },
        NamedLinks = new List<KeyValuePair<string, NamedLink>>
        {
            new ("link-name-1", new NamedLink { Link = "link-url", LinkType = LinkType.Url })
        }
    };
    
    public static readonly RoutingConfig InvalidRouteToConfig = new()
    {
        NamedRoutes = new List<KeyValuePair<string, NamedRoute>>
        {
            new ("route-1", new NamedRoute { LegacyLinkName = "link-name-1", BtmsLinkName = "link-name-1", SendRoutedResponseToFork = true, RouteTo = (RouteTo)99 })
        },
        NamedLinks = new List<KeyValuePair<string, NamedLink>>
        {
            new ("link-name-1", new NamedLink { Link = "http://link-url", LinkType = LinkType.Url })
        }
    };
    
    public static readonly RoutingConfig InvalidLinkTypeConfig = new()
    {
        NamedRoutes = new List<KeyValuePair<string, NamedRoute>>
        {
            new ("route-1", new NamedRoute { LegacyLinkName = "link-name-1", BtmsLinkName = "link-name-1", SendRoutedResponseToFork = true, RouteTo = RouteTo.Legacy })
        },
        NamedLinks = new List<KeyValuePair<string, NamedLink>>
        {
            new ("link-name-1", new NamedLink { Link = "http://link-url", LinkType = (LinkType)99 })
        }
    };
}
