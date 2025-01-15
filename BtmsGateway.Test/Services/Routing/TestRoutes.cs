using BtmsGateway.Services.Routing;

namespace BtmsGateway.Test.Services.Routing;

public static class TestRoutes
{
    public static readonly RoutingConfig RoutingConfig = new()
    {
        NamedRoutes = new Dictionary<string, NamedRoute>
        {
            { "route-1", new NamedRoute { LegacyLinkName = "legacy_link_name_1", BtmsLinkName = "btms_link_name_1", SendRoutedResponseToFork = true, RouteTo = RouteTo.Legacy } },
            { "route-2", new NamedRoute { LegacyLinkName = "legacy_link_name_2", BtmsLinkName = "btms_link_name_2", SendRoutedResponseToFork = true, RouteTo = RouteTo.Btms } }
        },
        NamedLinks = new Dictionary<string, NamedLink>
        {
            { "legacy_link_name_1", new NamedLink { Link = "http://legacy-link-url", LinkType = LinkType.Url, HostHeader = "legacy-host-header" } },
            { "legacy_link_name_2", new NamedLink { Link = "legacy-link-queue", LinkType = LinkType.Queue } },
            { "btms_link_name_1", new NamedLink { Link = "btms-link-queue", LinkType = LinkType.Queue } },
            { "btms_link_name_2", new NamedLink { Link = "http://btms-link-url", LinkType = LinkType.Url, HostHeader = "btms-host-header" } },
        }
    };
    
    public static readonly RoutingConfig InvalidUrlConfig = new()
    {
        NamedRoutes = new Dictionary<string, NamedRoute>
        {
            { "route-1", new NamedRoute { LegacyLinkName = "link_name_1", BtmsLinkName = "link_name_1", SendRoutedResponseToFork = true, RouteTo = RouteTo.Legacy } }
        },
        NamedLinks = new Dictionary<string, NamedLink>
        {
            { "link_name_1", new NamedLink { Link = "link-url", LinkType = LinkType.Url } }
        }
    };
    
    public static readonly RoutingConfig InvalidRouteToConfig = new()
    {
        NamedRoutes = new Dictionary<string, NamedRoute>
        {
            { "route-1", new NamedRoute { LegacyLinkName = "link_name_1", BtmsLinkName = "link_name_1", SendRoutedResponseToFork = true, RouteTo = (RouteTo)99 } }
        },
        NamedLinks = new Dictionary<string, NamedLink>
        {
            { "link_name_1", new NamedLink { Link = "http://link-url", LinkType = LinkType.Url } }
        }
    };
    
    public static readonly RoutingConfig InvalidLinkTypeConfig = new()
    {
        NamedRoutes = new Dictionary<string, NamedRoute>
        {
            { "route-1", new NamedRoute { LegacyLinkName = "link_name_1", BtmsLinkName = "link_name_1", SendRoutedResponseToFork = true, RouteTo = RouteTo.Legacy } }
        },
        NamedLinks = new Dictionary<string, NamedLink>
        {
            { "link_name_1", new NamedLink { Link = "http://link-url", LinkType = (LinkType)99 } }
        }
    };
}
