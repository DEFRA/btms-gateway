using ILogger = Serilog.ILogger;

namespace BtmsGateway.Services.Routing;

public interface IMessageRoutes
{
    RoutingResult GetRoute(string routePath);
}

public class MessageRoutes : IMessageRoutes
{
    private readonly ILogger _logger;
    private readonly RoutedUrl[] _routes;

    public MessageRoutes(RoutingConfig routingConfig, ILogger logger)
    {
        _logger = logger;
        try
        {
            _routes = routingConfig.Routes;
            if (_routes.Length != routingConfig.Routes.Select(x => x.Name).Distinct().Count()) throw new InvalidDataException("Duplicate route name(s)");
            if (_routes.Any(x => !Uri.TryCreate(x.LegacyUrl, UriKind.Absolute, out _))) throw new InvalidDataException("Legacy URL invalid");
            if (_routes.Any(x => !Uri.TryCreate(x.BtmsUrl, UriKind.Absolute, out _))) throw new InvalidDataException("BTMS URL invalid");
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
                ? new RoutingResult { RouteFound = false, RouteName = routeName }
                : route.RouteTo switch
                {
                    RouteTo.Legacy => new RoutingResult
                    {
                        RouteFound = true,
                        RouteName = routeName,
                        FullRouteUrl = $"{route.LegacyUrl}{routeUrlPath}",
                        FullForkUrl = $"{route.BtmsUrl}{routeUrlPath}",
                        RouteUrlPath = routeUrlPath,
                        SendRoutedResponseToFork = true
                    },
                    _ => new RoutingResult
                    {
                        RouteFound = true,
                        RouteName = routeName,
                        FullRouteUrl = $"{route.BtmsUrl}{routeUrlPath}",
                        FullForkUrl = $"{route.LegacyUrl}{routeUrlPath}",
                        RouteUrlPath = routeUrlPath,
                        SendRoutedResponseToFork = false
                    }
                };
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error getting route");
            throw;
        }
    }
}
