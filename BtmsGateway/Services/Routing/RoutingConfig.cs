namespace BtmsGateway.Services.Routing;

public record RoutingConfig
{
    public RoutedUrl[] AllRoutedRoutes => Routes.Concat(AdditionalRoutes).Join(NamedUrls, nr => nr.RoutedUrlName, nu => nu.Name, (nr, nu) => new RoutedUrl { Name = nr.Name, Url = nu.Url }).ToArray();
    public RoutedUrl[] AllForkedRoutes => Routes.Concat(AdditionalRoutes).Join(NamedUrls, nr => nr.ForkedUrlName, nu => nu.Name, (nr, nu) => new RoutedUrl { Name = nr.Name, Url = nu.Url }).ToArray();
    
    public required NamedRoute[] Routes { get; init; } = [];
    public required NamedRoute[] AdditionalRoutes { get; init; } = [];
    public required NamedUrl[] NamedUrls { get; init; } = [];
    public required HealthUrl[] HealthUrls { get; init; } = [];
}

public record NamedRoute
{
    public required string Name { get; init; }
    public required string RoutedUrlName { get; init; }
    public required string ForkedUrlName { get; init; }
}

public record NamedUrl
{
    public required string Name { get; init; }
    public required string Url { get; init; }
}

public record RoutedUrl
{
    public required string Name { get; init; }
    public required string Url { get; init; }
}

public record HealthUrl
{
    public required bool Disabled { get; init; }
    public required string Name { get; init; }
    public required string CheckType { get; init; } = "HTTP";
    public required string Method { get; init; }
    public required string Url { get; init; }
    public Uri Uri => new(Url);
}
