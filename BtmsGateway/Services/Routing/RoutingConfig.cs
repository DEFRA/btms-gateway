namespace BtmsGateway.Services.Routing;

public record RoutingConfig
{
    public RoutedUrl[] Routes =>
        NamedRoutes.Join(NamedUrls, nr => nr.Value.LegacyUrlName, nu => nu.Key, (nr, nu) => new { Name = nr.Key, LegacyUrl = nu.Value, nr.Value.BtmsUrlName, nr.Value.RouteTo })
                   .Join(NamedUrls, nr => nr.BtmsUrlName, nu => nu.Key, (nr, nu) => new RoutedUrl { Name = nr.Name, LegacyUrl = nr.LegacyUrl, BtmsUrl = nu.Value, RouteTo = nr.RouteTo })
                   .ToArray();
    
    public required Dictionary<string, NamedRoute> NamedRoutes { get; init; } = [];
    public required Dictionary<string, string> NamedUrls { get; init; } = [];
}

public record NamedRoute
{
    public required string LegacyUrlName { get; init; }
    public required string BtmsUrlName { get; init; }
    public required RouteTo RouteTo { get; init; }
}

public enum RouteTo { Legacy, Btms }

public record RoutedUrl
{
    public required string Name { get; init; }
    public required string LegacyUrl { get; init; }
    public required string BtmsUrl { get; init; }
    public required RouteTo RouteTo { get; init; }
}
