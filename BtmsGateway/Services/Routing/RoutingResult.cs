using System.Net;

namespace BtmsGateway.Services.Routing;

public record RoutingResult
{
    public static RoutingResult Empty => new();

    public string? RouteName { get; init; }
    public string? MessageSubXPath { get; set; }
    public string? Legend { get; init; }
    public bool RouteFound { get; init; }
    public bool RoutingSuccessful { get; init; }
    public LinkType RouteLinkType { get; init; }
    public string? FullRouteLink { get; init; }
    public string? RouteHostHeader { get; init; }
    public bool ConvertRoutedContentToFromJson { get; init; }
    public LinkType ForkLinkType { get; init; }
    public string? FullForkLink { get; init; }
    public string? ForkHostHeader { get; init; }
    public bool ConvertForkedContentToFromJson { get; init; }
    public string? UrlPath { get; init; }
    public string? ResponseContent { get; init; }
    public DateTimeOffset? ResponseDate { get; init; }
    public HttpStatusCode StatusCode { get; init; }
    public string? ErrorMessage { get; init; }
    public string? NamedProxy { get; init; }
}
