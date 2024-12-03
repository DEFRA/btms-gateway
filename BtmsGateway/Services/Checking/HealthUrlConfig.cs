namespace CdmsGateway.Services.Checking;

public record HealthUrlConfig
{
    public required Dictionary<string, HealthUrl> HealthUrls { get; init; } = [];
}

public record HealthUrl
{    
    public required string Name { get; init; }
    public required bool Disabled { get; init; }
    public required string CheckType { get; init; } = "HTTP";
    public required string Method { get; init; }
    public required string Url { get; init; }
    public Uri Uri => new(Url);
}
