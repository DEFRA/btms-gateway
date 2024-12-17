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
            if (routingConfig.NamedRoutes.Count != routingConfig.NamedRoutes.Select(x => x.Key).Distinct().Count()) throw new InvalidDataException("Duplicate route name(s) in config");
            if (routingConfig.NamedLinks.Count != routingConfig.NamedLinks.Select(x => x.Key).Distinct().Count()) throw new InvalidDataException("Duplicate link name(s) in config");
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
   
    public RoutingResult GetRoute(string routePath)
    {
        try
        {
            var routeParts = routePath.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (routeParts.Length == 0) return new RoutingResult();
        
            var routeName = routeParts[0].ToLower();
            var routeUrlPath = $"/{string.Join('/', routeParts[1..])}";
            var route = _routes.SingleOrDefault(x => x.Name == routeName);

            return route == null
                ? new RoutingResult { RouteFound = false, RouteName = routeName, UrlPath = routeUrlPath }
                : route.RouteTo switch
                {
                    RouteTo.Legacy => new RoutingResult
                    {
                        RouteFound = true,
                        RouteName = routeName,
                        FullRouteLink = $"{route.LegacyLink}{(route.LegacyLinkType == LinkType.Url ? routeUrlPath : null)}",
                        RouteLinkType = route.LegacyLinkType,
                        FullForkLink = $"{route.BtmsLink}{(route.BtmsLinkType == LinkType.Url ? routeUrlPath : null)}",
                        ForkLinkType = route.BtmsLinkType,
                        UrlPath = routeUrlPath,
                        SendLegacyResponseToBtms = route.SendLegacyResponseToBtms
                    },
                    RouteTo.Btms => new RoutingResult
                    {
                        RouteFound = true,
                        RouteName = routeName,
                        FullRouteLink = $"{route.BtmsLink}{(route.BtmsLinkType == LinkType.Url ? routeUrlPath : null)}",
                        RouteLinkType = route.BtmsLinkType,
                        FullForkLink = $"{route.LegacyLink}{(route.LegacyLinkType == LinkType.Url ? routeUrlPath : null)}",
                        ForkLinkType = route.LegacyLinkType,
                        UrlPath = routeUrlPath,
                        SendLegacyResponseToBtms = false
                    },
                    _ => throw new ArgumentOutOfRangeException(nameof(route.RouteTo), "Can only route to 'Legacy' or 'Btms'")
                };
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error getting route");
            throw;
        }
    }
}
