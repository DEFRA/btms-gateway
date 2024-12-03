using System.Net;

namespace BtmsGateway.Services.Routing;

public record RoutingResult
{
    public string? RouteName { get; init; }
    public bool RouteFound { get; init; }
    public bool RoutingSuccessful { get; init; }
    public string? FullRouteUrl { get; init; }
    public string? FullForkUrl { get; set; }
    public string? RouteUrlPath { get; init; }
    public bool SendRoutedResponseToFork { get; set; }
    public string? ResponseContent { get; init; }
    public DateTimeOffset? ResponseDate { get; init; }
    public HttpStatusCode StatusCode { get; init; }
}