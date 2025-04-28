namespace BtmsGateway.Services.Checking;

public record HealthCheckConfig
{
    public required bool Disabled { get; init; }
    public required bool AutomatedHealthCheckDisabled { get; init; }
    public required Dictionary<string, HealthCheckUrl> Urls { get; init; } = [];
}

public record HealthCheckUrl
{
    public required bool Disabled { get; init; }
    public required string Method { get; init; }
    public required string Url { get; init; }
    public string? HostHeader { get; init; }
    public required bool IncludeInAutomatedHealthCheck { get; init; }
}

public record CheckRouteUrl
{
    public required string Name { get; init; }
    public required bool Disabled { get; init; }
    public required string CheckType { get; init; }
    public required string Method { get; init; }
    public string? HostHeader { get; init; }
    public required string Url { get; init; }
    public Uri Uri => new(Url);
}
