using System.Net;

namespace BtmsGateway.Services.Routing;

public record RoutingResult
{
    public string? RouteName { get; init; }
    public bool RouteFound { get; init; }
    public bool RoutingSuccessful { get; init; }
    public string? FullRouteLink { get; init; }
    public LinkType RouteLinkType { get; init; }
    public bool ConvertedRoutedContentToJson { get; init; }
    public string? FullForkLink { get; init; }
    public LinkType ForkLinkType { get; init; }
    public bool ConvertedForkedContentToJson { get; init; }
    public string? UrlPath { get; init; }
    public bool SendLegacyResponseToBtms { get; init; }
    public string? ResponseContent { get; init; }
    public DateTimeOffset? ResponseDate { get; init; }
    public HttpStatusCode StatusCode { get; init; }
}