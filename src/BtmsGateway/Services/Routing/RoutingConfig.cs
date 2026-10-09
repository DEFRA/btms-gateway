namespace BtmsGateway.Services.Routing;

public record RoutingConfig
{
    public bool AutomatedHealthCheckDisabled { get; init; }

    public RoutedLink[] AllRoutes => GetAllRoutes();

    private RoutedLink[] GetAllRoutes()
    {
        return NamedRoutes
            .Join(
                NamedLinks,
                nr => nr.Value.BtmsLinkName,
                nl => nl.Key,
                (nr, nl) =>
                    new RoutedLink
                    {
                        Name = nr.Key,
                        BtmsLink = nl.Value.Link.TrimEnd('/'),
                        BtmsHostHeader = nl.Value.HostHeader,
                        RoutePath = nr.Value.RoutePath.Trim('/'),
                        MessageSubXPath = nr.Value.MessageSubXPath,
                        Legend = nr.Value.Legend,
                        IsCds = nr.Value.IsCds,
                        NamedProxy = nr.Value.NamedProxy,
                    }
            )
            .ToArray();
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
    public string? BtmsLinkName { get; init; }
    public bool IsCds { get; init; }
    public string? NamedProxy { get; init; }
}

public record NamedLink
{
    public required string Link { get; init; }
    public string? HostHeader { get; init; }
}

public record Destination
{
    public required string Link { get; init; }
    public required string RoutePath { get; init; }
    public required string ContentType { get; init; }
    public string? HostHeader { get; init; }
    public string? Method { get; init; }
}

public record RoutedLink
{
    public required string Name { get; init; }
    public required string Legend { get; init; }
    public required string RoutePath { get; init; }
    public required string MessageSubXPath { get; init; }
    public string? BtmsLink { get; init; }
    public string? BtmsHostHeader { get; init; }
    public required bool IsCds { get; init; }
    public string? NamedProxy { get; init; }
}
