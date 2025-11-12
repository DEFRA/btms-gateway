using BtmsGateway.Services.Routing;

namespace BtmsGateway.Test.Services.Routing;

public static class TestRoutes
{
    public static readonly RoutingConfig RoutingConfig = new()
    {
        NamedRoutes = new Dictionary<string, NamedRoute>
        {
            {
                "route-1",
                new NamedRoute
                {
                    RoutePath = "/route/path-1/sub/path",
                    Legend = "Route 1",
                    BtmsLinkName = "btms_link_name_1",
                    MessageSubXPath = "Message1",
                }
            },
            {
                "route-2",
                new NamedRoute
                {
                    RoutePath = "/route/path-2/sub/path",
                    Legend = "Route 2",
                    BtmsLinkName = "btms_link_name_2",
                    MessageSubXPath = "Message2",
                }
            },
            {
                "route-3",
                new NamedRoute
                {
                    RoutePath = "/route/path-3/sub/path",
                    Legend = "Route 3",
                    BtmsLinkName = "none",
                    MessageSubXPath = "Message3",
                }
            },
            {
                "route-4",
                new NamedRoute
                {
                    RoutePath = "/route/path-4/sub/path",
                    Legend = "Route 4",
                    BtmsLinkName = "btms_link_name_2",
                    MessageSubXPath = "Message4",
                }
            },
        },
        NamedLinks = new Dictionary<string, NamedLink>
        {
            {
                "btms_link_name_1",
                new NamedLink { Link = "btms-link-queue", LinkType = LinkType.Queue }
            },
            {
                "btms_link_name_2",
                new NamedLink
                {
                    Link = "http://btms-link-url",
                    LinkType = LinkType.Url,
                    HostHeader = "btms-host-header",
                }
            },
            {
                "none",
                new NamedLink
                {
                    Link = "none",
                    LinkType = LinkType.None,
                    HostHeader = null,
                }
            },
        },
        Destinations = new Dictionary<string, Destination>
        {
            {
                "destination-1",
                new Destination
                {
                    LinkType = LinkType.Url,
                    Link = "http://destination-url",
                    RoutePath = "/route/path-1",
                    ContentType = "application/soap+xml",
                    HostHeader = "syst32.hmrc.gov.uk",
                    Method = "POST",
                }
            },
        },
    };

    public static readonly RoutingConfig DifferentMessageTypesOnSameRoutingConfig = new()
    {
        NamedRoutes = new Dictionary<string, NamedRoute>
        {
            {
                "route-1",
                new NamedRoute
                {
                    RoutePath = "/route/path-A/sub/path",
                    BtmsLinkName = "btms_link_name_1",
                    MessageSubXPath = "Message1/Message",
                    Legend = "Route 1",
                }
            },
            {
                "route-2",
                new NamedRoute
                {
                    RoutePath = "/route/path-A/sub/path",
                    BtmsLinkName = "btms_link_name_2",
                    MessageSubXPath = "Message2/Message",
                    Legend = "Route 2",
                }
            },
        },
        NamedLinks = new Dictionary<string, NamedLink>
        {
            {
                "legacy_link_name_1",
                new NamedLink { Link = "http://legacy-link-1-url", LinkType = LinkType.Url }
            },
            {
                "legacy_link_name_2",
                new NamedLink { Link = "http://legacy-link-2-url", LinkType = LinkType.Url }
            },
            {
                "btms_link_name_1",
                new NamedLink { Link = "btms-link-1-queue", LinkType = LinkType.Queue }
            },
            {
                "btms_link_name_2",
                new NamedLink
                {
                    Link = "btms-link-2-queue",
                    LinkType = LinkType.Queue,
                    HostHeader = "btms-host-header",
                }
            },
        },
        Destinations = new Dictionary<string, Destination>
        {
            {
                "destination-1",
                new Destination
                {
                    LinkType = LinkType.Url,
                    Link = "http://destination-url",
                    RoutePath = "/route/path-1",
                    ContentType = "application/soap+xml",
                    HostHeader = "syst32.hmrc.gov.uk",
                    Method = "POST",
                }
            },
        },
    };

    public static readonly RoutingConfig InvalidUrlConfig = new()
    {
        NamedRoutes = new Dictionary<string, NamedRoute>
        {
            {
                "route-1",
                new NamedRoute
                {
                    RoutePath = "/route/path-1",
                    BtmsLinkName = "link_name_1",
                    MessageSubXPath = "Message1",
                    Legend = "legend",
                }
            },
        },
        NamedLinks = new Dictionary<string, NamedLink>
        {
            {
                "link_name_1",
                new NamedLink { Link = "link-url", LinkType = LinkType.Url }
            },
        },
        Destinations = new Dictionary<string, Destination>
        {
            {
                "destination-1",
                new Destination
                {
                    LinkType = LinkType.Url,
                    Link = "http://destination-url",
                    RoutePath = "/route/path-1",
                    ContentType = "application/soap+xml",
                    HostHeader = "syst32.hmrc.gov.uk",
                    Method = "POST",
                }
            },
        },
    };

    public static readonly RoutingConfig InvalidRouteToConfig = new()
    {
        NamedRoutes = new Dictionary<string, NamedRoute>
        {
            {
                "route-1",
                new NamedRoute
                {
                    RoutePath = "/route/path-1",
                    BtmsLinkName = "link_name_1",
                    MessageSubXPath = "Message1",
                    Legend = "legend",
                }
            },
        },
        NamedLinks = new Dictionary<string, NamedLink>
        {
            {
                "link_name_1",
                new NamedLink { Link = "http://link-url", LinkType = LinkType.Url }
            },
        },
        Destinations = new Dictionary<string, Destination>
        {
            {
                "destination-1",
                new Destination
                {
                    LinkType = LinkType.Url,
                    Link = "http://destination-url",
                    RoutePath = "/route/path-1",
                    ContentType = "application/soap+xml",
                    HostHeader = "syst32.hmrc.gov.uk",
                    Method = "POST",
                }
            },
        },
    };

    public static readonly RoutingConfig InvalidLinkTypeConfig = new()
    {
        NamedRoutes = new Dictionary<string, NamedRoute>
        {
            {
                "route-1",
                new NamedRoute
                {
                    RoutePath = "/route/path-1",
                    BtmsLinkName = "link_name_1",
                    MessageSubXPath = "Message1",
                    Legend = "legend",
                }
            },
        },
        NamedLinks = new Dictionary<string, NamedLink>
        {
            {
                "link_name_1",
                new NamedLink { Link = "http://link-url", LinkType = (LinkType)99 }
            },
        },
        Destinations = new Dictionary<string, Destination>
        {
            {
                "destination-1",
                new Destination
                {
                    LinkType = LinkType.Url,
                    Link = "http://destination-url",
                    RoutePath = "/route/path-1",
                    ContentType = "application/soap+xml",
                    HostHeader = "syst32.hmrc.gov.uk",
                    Method = "POST",
                }
            },
        },
    };

    public static readonly RoutingConfig CdsDefinedRoutes = new()
    {
        NamedRoutes = new Dictionary<string, NamedRoute>
        {
            {
                "cds-route",
                new NamedRoute
                {
                    RoutePath = "/route/path-1/sub/path",
                    Legend = "Route 1",
                    BtmsLinkName = "none",
                    MessageSubXPath = "Message1",
                    IsCds = true,
                }
            },
            {
                "some-other-route",
                new NamedRoute
                {
                    RoutePath = "/route/path-2/sub/path",
                    Legend = "Route 2",
                    BtmsLinkName = "none",
                    MessageSubXPath = "Message2",
                }
            },
        },
        NamedLinks = new Dictionary<string, NamedLink>
        {
            {
                "none",
                new NamedLink
                {
                    Link = "none",
                    LinkType = LinkType.None,
                    HostHeader = null,
                }
            },
        },
        Destinations = new Dictionary<string, Destination>(),
    };
}
