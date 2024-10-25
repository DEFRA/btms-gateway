using System.Net;

namespace CdmsGateway.Services.Routing;

public class RoutingResult
{
    public bool RouteFound { get; init; }
    public bool RoutedSuccessfully { get; init; }
    public string? RouteUrl { get; init; }
    public string? ResponseContent { get; init; }
    public HttpStatusCode StatusCode { get; init; }
}