namespace BtmsGateway.Services.Routing;

public record RoutingConfig
{
    public bool AutomatedHealthCheckDisabled { get; set; }

    public RoutedLink[] AllRoutes => GetAllRoutes();

    private RoutedLink[] GetAllRoutes()
    {
        var legacy = NamedRoutes.Join(NamedLinks, nr => nr.Value.LegacyLinkName, nl => nl.Key, (nr, nl) => new
        {
            Name = nr.Key,
            nl.Value.Link,
            nl.Value.LinkType,
            nl.Value.HostHeader,
            nr.Value.RoutePath,
            nr.Value.MessageBodyDepth,
            nr.Value.SendLegacyResponseToBtms,
            nr.Value.RouteTo
        });
        var btms = NamedRoutes.Join(NamedLinks, nr => nr.Value.BtmsLinkName, nl => nl.Key, (nr, nl) => new
        {
            Name = nr.Key,
            nl.Value.Link,
            nl.Value.LinkType,
            nl.Value.HostHeader,
            nr.Value.RoutePath,
            nr.Value.MessageBodyDepth,
            nr.Value.SendLegacyResponseToBtms,
            nr.Value.RouteTo
        });
        var output = legacy.Join(btms, l => l.Name, b => b.Name, (l, b) => new RoutedLink
        {
            Name = l.Name,
            LegacyLink = l.Link.TrimEnd('/'),
            LegacyLinkType = l.LinkType,
            LegacyHostHeader = l.HostHeader,
            BtmsLink = b.Link.TrimEnd('/'),
            BtmsLinkType = b.LinkType,
            BtmsHostHeader = b.HostHeader,
            RoutePath = l.RoutePath.Trim('/'),
            MessageBodyDepth = l.MessageBodyDepth,
            SendLegacyResponseToBtms = b.SendLegacyResponseToBtms,
            RouteTo = b.RouteTo
        })
            .ToArray();

        return output;
    }

    public required Dictionary<string, NamedRoute> NamedRoutes { get; init; } = [];
    public required Dictionary<string, NamedLink> NamedLinks { get; init; } = [];
}

public record NamedRoute
{
    public required string RoutePath { get; init; }
    public string? LegacyLinkName { get; init; }
    public string? BtmsLinkName { get; init; }
    public required bool SendLegacyResponseToBtms { get; init; }
    public int MessageBodyDepth { get; init; } = 1;
    public required RouteTo RouteTo { get; init; }
}

public record NamedLink
{
    public required string Link { get; init; }
    public required LinkType LinkType { get; init; }
    public string? HostHeader { get; init; }
}

public enum LinkType { None, Url, Queue }

public record RoutedLink
{
    public required string Name { get; init; }
    public required string RoutePath { get; init; }
    public string? LegacyLink { get; init; }
    public required LinkType LegacyLinkType { get; init; }
    public string? LegacyHostHeader { get; init; }
    public string? BtmsLink { get; init; }
    public required LinkType BtmsLinkType { get; init; }
    public string? BtmsHostHeader { get; init; }
    public int MessageBodyDepth { get; init; } = 1;
    public required bool SendLegacyResponseToBtms { get; init; }
    public required RouteTo RouteTo { get; init; }
}

public enum RouteTo { Legacy, Btms }