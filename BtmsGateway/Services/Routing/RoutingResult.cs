using System.Net;

namespace BtmsGateway.Services.Routing;

public record RoutingResult
{
    public string? RouteName { get; init; }
    public bool RouteFound { get; init; }
    public bool RoutingSuccessful { get; init; }
    public LinkType RouteLinkType { get; init; }
    public string? FullRouteLink { get; init; }
    public string? RouteHostHeader { get; init; }
    public bool ConvertRoutedContentToJson { get; init; }
    public LinkType ForkLinkType { get; init; }
    public string? FullForkLink { get; init; }
    public string? ForkHostHeader { get; init; }
    public bool ConvertForkedContentToJson { get; init; }
    public string? UrlPath { get; init; }
    public string? ResponseContent { get; init; }
    public DateTimeOffset? ResponseDate { get; init; }
    public HttpStatusCode StatusCode { get; init; }
}