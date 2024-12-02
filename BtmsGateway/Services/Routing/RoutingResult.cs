using System.Net;

namespace BtmsGateway.Services.Routing;

public record RoutingResult
{
    public string? RouteName { get; init; }
    public bool RouteFound { get; init; }
    public bool RoutingSuccessful { get; init; }
    public string? RouteUrl { get; init; }
    public string? RouteUrlPath { get; init; }
    public string? ResponseContent { get; init; }
    public DateTimeOffset? ResponseDate { get; init; }
    public HttpStatusCode StatusCode { get; init; }
}