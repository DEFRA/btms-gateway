using BtmsGateway.Services.Routing;

namespace BtmsGateway.Test.Services.Routing;

public static class TestRoutes
{
    public static readonly RoutingConfig RoutingConfig = new()
    {
        NamedRoutes = new Dictionary<string, NamedRoute>
        {
            { "route-1", new NamedRoute { RoutePath = "/route/path-1/sub/path", Legend = "Route 1", LegacyLinkName = "legacy_link_name_1", BtmsLinkName = "btms_link_name_1", MessageSubXPath = "Message1", RouteTo = RouteTo.Legacy } },
            { "route-2", new NamedRoute { RoutePath = "/route/path-2/sub/path", Legend = "Route 2", LegacyLinkName = "legacy_link_name_2", BtmsLinkName = "btms_link_name_2", MessageSubXPath = "Message2", RouteTo = RouteTo.Btms } },
            { "route-3", new NamedRoute { RoutePath = "/route/path-3/sub/path", Legend = "Route 3", LegacyLinkName = "legacy_link_name_1", BtmsLinkName = "none", MessageSubXPath = "Message3", RouteTo = RouteTo.Legacy } },
            { "route-4", new NamedRoute { RoutePath = "/route/path-4/sub/path", Legend = "Route 4", LegacyLinkName = "none", BtmsLinkName = "btms_link_name_2", MessageSubXPath = "Message4", RouteTo = RouteTo.Btms } }
        },
        NamedLinks = new Dictionary<string, NamedLink>
        {
            { "legacy_link_name_1", new NamedLink { Link = "http://legacy-link-url", LinkType = LinkType.Url, HostHeader = "legacy-host-header" } },
            { "legacy_link_name_2", new NamedLink { Link = "legacy-link-queue", LinkType = LinkType.Queue } },
            { "btms_link_name_1", new NamedLink { Link = "btms-link-queue", LinkType = LinkType.Queue } },
            { "btms_link_name_2", new NamedLink { Link = "http://btms-link-url", LinkType = LinkType.Url, HostHeader = "btms-host-header" } },
            { "none", new NamedLink { Link = "none", LinkType = LinkType.None, HostHeader = null } },
        }
    };

    public static readonly RoutingConfig DifferentMessageTypesOnSameRoutingConfig = new()
    {
        NamedRoutes = new Dictionary<string, NamedRoute>
        {
            { "route-1", new NamedRoute { RoutePath = "/route/path-A/sub/path", LegacyLinkName = "legacy_link_name_1", BtmsLinkName = "btms_link_name_1", MessageSubXPath = "Message1/Message", Legend = "Route 1", RouteTo = RouteTo.Legacy } },
            { "route-2", new NamedRoute { RoutePath = "/route/path-A/sub/path", LegacyLinkName = "legacy_link_name_2", BtmsLinkName = "btms_link_name_2", MessageSubXPath = "Message2/Message", Legend = "Route 2", RouteTo = RouteTo.Legacy } }
        },
        NamedLinks = new Dictionary<string, NamedLink>
        {
            { "legacy_link_name_1", new NamedLink { Link = "http://legacy-link-1-url", LinkType = LinkType.Url } },
            { "legacy_link_name_2", new NamedLink { Link = "http://legacy-link-2-url", LinkType = LinkType.Url } },
            { "btms_link_name_1", new NamedLink { Link = "btms-link-1-queue", LinkType = LinkType.Queue } },
            { "btms_link_name_2", new NamedLink { Link = "btms-link-2-queue", LinkType = LinkType.Queue, HostHeader = "btms-host-header" } }
        }
    };

    public static readonly RoutingConfig InvalidUrlConfig = new()
    {
        NamedRoutes = new Dictionary<string, NamedRoute>
        {
            { "route-1", new NamedRoute { RoutePath = "/route/path-1", LegacyLinkName = "link_name_1", BtmsLinkName = "link_name_1", MessageSubXPath = "Message1", Legend = "legend", RouteTo = RouteTo.Legacy } }
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
            { "route-1", new NamedRoute { RoutePath = "/route/path-1", LegacyLinkName = "link_name_1", BtmsLinkName = "link_name_1", MessageSubXPath = "Message1", Legend = "legend", RouteTo = (RouteTo)99 } }
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
            { "route-1", new NamedRoute { RoutePath = "/route/path-1", LegacyLinkName = "link_name_1", BtmsLinkName = "link_name_1", MessageSubXPath = "Message1", Legend = "legend", RouteTo = RouteTo.Legacy } }
        },
        NamedLinks = new Dictionary<string, NamedLink>
        {
            { "link_name_1", new NamedLink { Link = "http://link-url", LinkType = (LinkType)99 } }
        }
    };
}