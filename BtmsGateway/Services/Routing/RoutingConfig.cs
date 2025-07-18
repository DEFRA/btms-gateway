namespace BtmsGateway.Services.Routing;

public record RoutingConfig
{
    public bool AutomatedHealthCheckDisabled { get; init; }

    public RoutedLink[] AllRoutes => GetAllRoutes();

    private RoutedLink[] GetAllRoutes()
    {
        var legacy = NamedRoutes.Join(
            NamedLinks,
            nr => nr.Value.LegacyLinkName,
            nl => nl.Key,
            (nr, nl) =>
                new
                {
                    Name = nr.Key,
                    nl.Value.Link,
                    nl.Value.LinkType,
                    nl.Value.HostHeader,
                    nr.Value.RoutePath,
                    nr.Value.MessageSubXPath,
                    nr.Value.Legend,
                    nr.Value.RouteTo,
                    nr.Value.IsCds,
                }
        );
        var btms = NamedRoutes.Join(
            NamedLinks,
            nr => nr.Value.BtmsLinkName,
            nl => nl.Key,
            (nr, nl) =>
                new
                {
                    Name = nr.Key,
                    nl.Value.Link,
                    nl.Value.LinkType,
                    nl.Value.HostHeader,
                    nr.Value.RoutePath,
                    nr.Value.MessageSubXPath,
                    nr.Value.Legend,
                    nr.Value.RouteTo,
                    nr.Value.IsCds,
                }
        );
        var output = legacy
            .Join(
                btms,
                l => l.Name,
                b => b.Name,
                (l, b) =>
                    new RoutedLink
                    {
                        Name = l.Name,
                        Legend = l.Legend,
                        LegacyLink = l.Link.TrimEnd('/'),
                        LegacyLinkType = l.LinkType,
                        LegacyHostHeader = l.HostHeader,
                        BtmsLink = b.Link.TrimEnd('/'),
                        BtmsLinkType = b.LinkType,
                        BtmsHostHeader = b.HostHeader,
                        RoutePath = l.RoutePath.Trim('/'),
                        MessageSubXPath = l.MessageSubXPath,
                        RouteTo = b.RouteTo,
                        IsCds = b.IsCds,
                    }
            )
            .ToArray();

        return output;
    }

    public required Dictionary<string, NamedRoute> NamedRoutes { get; init; } = [];
    public required Dictionary<string, NamedLink> NamedLinks { get; init; } = [];
    public required Dictionary<string, Destination> Destinations { get; init; } = [];
}

public record NamedRoute
{
    public required string RoutePath { get; init; }
    public required string Legend { get; init; }
    public required string MessageSubXPath { get; init; }
    public string? LegacyLinkName { get; init; }
    public string? BtmsLinkName { get; init; }
    public required RouteTo RouteTo { get; init; }
    public bool IsCds { get; init; }
}

public record NamedLink
{
    public required string Link { get; init; }
    public required LinkType LinkType { get; init; }
    public string? HostHeader { get; init; }
}

public record Destination
{
    public required LinkType LinkType { get; init; }
    public required string Link { get; init; }
    public required string RoutePath { get; init; }
    public required string ContentType { get; init; }
    public string? HostHeader { get; init; }
    public string? Method { get; init; }
}

public enum LinkType
{
    None,
    Url,
    Queue,
    DecisionComparer,
    DecisionComparerErrorNotifications,
}

public record RoutedLink
{
    public required string Name { get; init; }
    public required string Legend { get; init; }
    public required string RoutePath { get; init; }
    public required string MessageSubXPath { get; init; }
    public string? LegacyLink { get; init; }
    public required LinkType LegacyLinkType { get; init; }
    public string? LegacyHostHeader { get; init; }
    public string? BtmsLink { get; init; }
    public required LinkType BtmsLinkType { get; init; }
    public string? BtmsHostHeader { get; init; }
    public required RouteTo RouteTo { get; init; }
    public required bool IsCds { get; init; }
}

public enum RouteTo
{
    Legacy,
    Btms,
}
