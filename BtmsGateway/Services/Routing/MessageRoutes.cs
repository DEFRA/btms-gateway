using ILogger = Serilog.ILogger;

namespace BtmsGateway.Services.Routing;

public interface IMessageRoutes
{
    RoutingResult GetRoutedRoute(string routePath);
    RoutingResult GetForkedRoute(string routePath);
    HealthUrl[] HealthUrls { get; }
}

static class RepeatedExtension
{
    public static IEnumerable<TResult> Repeated<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, TResult> selector) where TResult : notnull
    {
        var distinct = new Dictionary<TResult, int>();
        foreach (var sourceItem in source)
        {
            var item = selector(sourceItem);
            if (!distinct.ContainsKey(item))
                distinct.Add(item, 1);
            else
            {
                if (distinct[item]++ == 1) // only yield items on first repeated occurence
                    yield return item;
            }                    
        }
    }
}

public class MessageRoutes : IMessageRoutes
{
    private readonly ILogger _logger;
    private readonly IDictionary<string, string> _routedRoutes;
    private readonly IDictionary<string, string> _forkedRoutes;

    public MessageRoutes(RoutingConfig routingConfig, ILogger logger)
    {
        _logger = logger;
        try
        {
            var repeatedRoutes = routingConfig.AllRoutedRoutes.Repeated(r => r.Name).ToArray();
            var repeatedForks = routingConfig.AllForkedRoutes.Repeated(r => r.Name).ToArray();
            
            if (repeatedRoutes.Length > 0) throw new InvalidDataException($"Duplicate routed route name(s) {repeatedRoutes}");
            if (repeatedForks.Length > 0) throw new InvalidDataException($"Duplicate forked route name(s) {repeatedForks}");
            
            if (routingConfig.AllRoutedRoutes.Any(x => !Uri.TryCreate(x.Url, UriKind.Absolute, out _))) throw new InvalidDataException("Routed URL invalid");
            if (routingConfig.AllForkedRoutes.Any(x => !Uri.TryCreate(x.Url, UriKind.Absolute, out _))) throw new InvalidDataException("Forked URL invalid");

            _routedRoutes = routingConfig.AllRoutedRoutes.ToDictionary(x => x.Name.ToLower(), x => x.Url.Trim('/'));
            _forkedRoutes = routingConfig.AllForkedRoutes.ToDictionary(x => x.Name.ToLower(), x => x.Url.Trim('/'));
            HealthUrls = routingConfig.HealthUrls;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error creating routing table");
            throw;
        }
    }

    public HealthUrl[] HealthUrls { get; }
    
    public RoutingResult GetRoutedRoute(string routePath) => GetRoute(routePath, _routedRoutes);

    public RoutingResult GetForkedRoute(string routePath) => GetRoute(routePath, _forkedRoutes);

    private RoutingResult GetRoute(string routePath, IDictionary<string, string> routes)
    {
        try
        {
            var routeParts = routePath.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (routeParts.Length == 0) return new RoutingResult();
        
            var routeName = routeParts[0].ToLower();
            var routeUrlPath = $"/{string.Join('/', routeParts[1..])}";
            var routeUrl = routes.TryGetValue(routeName, out var url) ? $"{url}{routeUrlPath}" : null;

            return new RoutingResult { RouteFound = routeUrl != null, RouteName = routeName, RouteUrl = routeUrl, RouteUrlPath = routeUrlPath };
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error getting route");
            throw;
        }
    }
}
