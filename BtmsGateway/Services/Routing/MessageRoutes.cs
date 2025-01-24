using System.Diagnostics.CodeAnalysis;
using System.Net;
using ILogger = Serilog.ILogger;

namespace BtmsGateway.Services.Routing;

public interface IMessageRoutes
{
    RoutingResult GetRoute(string routePath);
}

public class MessageRoutes : IMessageRoutes
{
    private readonly ILogger _logger;
    private readonly RoutedLink[] _routes;

    public MessageRoutes(RoutingConfig routingConfig, ILogger logger)
    {
        _logger = logger;
        try
        {
            if (routingConfig.NamedLinks.Any(x => x.Value.LinkType == LinkType.Url && !Uri.TryCreate(x.Value.Link, UriKind.Absolute, out _))) throw new InvalidDataException("Invalid URL(s) in config");
            if (routingConfig.NamedRoutes.Any(x => !Enum.IsDefined(typeof(RouteTo), x.Value.RouteTo))) throw new InvalidDataException("Invalid Route To in config");
            if (routingConfig.NamedLinks.Any(x => !Enum.IsDefined(typeof(LinkType), x.Value.LinkType))) throw new InvalidDataException("Invalid Link Type in config");
            _routes = routingConfig.AllRoutes;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error creating routing table");
            throw;
        }
    }
   
    [SuppressMessage("SonarLint", "S3358", Justification = "The second nested ternary in each case (lines 55, 56, 68, 69) is within a string interpolation so is very clearly independent of the first")]
    public RoutingResult GetRoute(string routePath)
    {
        var routeName = "";
        var routeUrlPath = "";
        try
        {
            var routeParts = routePath.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (routeParts.Length == 0) return new RoutingResult();
        
            routeName = routeParts[0].ToLower();
            routeUrlPath = $"/{string.Join('/', routeParts[1..])}";
            var route = _routes.SingleOrDefault(x => x.Name == routeName);

            return route == null
                ? new RoutingResult { RouteFound = false, RouteName = routeName, UrlPath = routeUrlPath, StatusCode = HttpStatusCode.InternalServerError, ErrorMessage = "Route not found" }
                : route.RouteTo switch
                {
                    RouteTo.Legacy => new RoutingResult
                    {
                        RouteFound = true,
                        RouteName = routeName,
                        RouteLinkType = route.LegacyLinkType,
                        ForkLinkType = route.BtmsLinkType,
                        FullRouteLink = route.LegacyLinkType == LinkType.None ? null : $"{route.LegacyLink}{(route.LegacyLinkType == LinkType.Url ? routeUrlPath : null)}",
                        FullForkLink = route.BtmsLinkType == LinkType.None ? null : $"{route.BtmsLink}{(route.BtmsLinkType == LinkType.Url ? routeUrlPath : null)}",
                        RouteHostHeader = route.LegacyHostHeader,
                        ForkHostHeader = route.BtmsHostHeader,
                        ConvertForkedContentToJson = true,
                        UrlPath = routeUrlPath
                    },
                    RouteTo.Btms => new RoutingResult
                    {
                        RouteFound = true,
                        RouteName = routeName,
                        RouteLinkType = route.BtmsLinkType,
                        ForkLinkType = route.LegacyLinkType,
                        FullRouteLink = route.BtmsLinkType == LinkType.None ? null : $"{route.BtmsLink}{(route.BtmsLinkType == LinkType.Url ? routeUrlPath : null)}",
                        FullForkLink = route.LegacyLinkType == LinkType.None ? null : $"{route.LegacyLink}{(route.LegacyLinkType == LinkType.Url ? routeUrlPath : null)}",
                        RouteHostHeader = route.BtmsHostHeader,
                        ForkHostHeader = route.LegacyHostHeader,
                        ConvertRoutedContentToJson = true,
                        UrlPath = routeUrlPath
                    },
                    _ => throw new ArgumentOutOfRangeException(nameof(route.RouteTo), "Can only route to 'Legacy' or 'Btms'")
                };
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error getting route");
            return new RoutingResult { RouteFound = false, RouteName = routeName, UrlPath = routeUrlPath, StatusCode = HttpStatusCode.InternalServerError, ErrorMessage = ex.Message };
        }
    }
}
