namespace BtmsGateway.Services.Routing;

public record RoutingConfig
{
    public RoutedLink[] AllRoutes =>
        NamedRoutes.Join(NamedLinks, nr => nr.Value.LegacyLinkName, nl => nl.Key, (nr, nl) => new { Name = nr.Key, nr.Value.BtmsLinkName, LegacyLink = nl.Value.Link, LegacyLinkType = nl.Value.LinkType, nr.Value.SendRoutedResponseToFork, nr.Value.RouteTo })
                   .Join(NamedLinks, nrl => nrl.BtmsLinkName, nl => nl.Key, (nrl, nl) => new RoutedLink { Name = nrl.Name, LegacyLink = nrl.LegacyLink.TrimEnd('/'), LegacyLinkType = nrl.LegacyLinkType, BtmsLink = nl.Value.Link.TrimEnd('/'), BtmsLinkType = nl.Value.LinkType, SendLegacyResponseToBtms = nrl.SendRoutedResponseToFork, RouteTo = nrl.RouteTo })
                   .ToArray();
    
    public required Dictionary<string, NamedRoute> NamedRoutes { get; init; } = [];
    public required Dictionary<string, NamedLink> NamedLinks { get; init; } = [];
}

public record NamedRoute
{
    public required string LegacyLinkName { get; init; }
    public required string BtmsLinkName { get; init; }
    public required bool SendRoutedResponseToFork { get; init; }
    public required RouteTo RouteTo { get; init; }
}

public record NamedLink
{
    public required string Link { get; init; }
    public required LinkType LinkType { get; init; }
}

public enum LinkType { Url, Queue }

public record RoutedLink
{
    public required string Name { get; init; }
    public required string LegacyLink { get; init; }
    public required LinkType LegacyLinkType { get; init; }
    public required string BtmsLink { get; init; }
    public required LinkType BtmsLinkType { get; init; }
    public required bool SendLegacyResponseToBtms { get; init; }
    public required RouteTo RouteTo { get; init; }
}

public enum RouteTo { Legacy, Btms }