using System.Diagnostics.CodeAnalysis;
using System.Net;
using BtmsGateway.Exceptions;
using BtmsGateway.Services.Converter;

namespace BtmsGateway.Services.Routing;

public interface IMessageRoutes
{
    RoutingResult GetRoute(string routePath, SoapContent soapContent, string? correlationId, string? mrn);
    bool IsCdsRoute(string routePath);
}

public class MessageRoutes : IMessageRoutes
{
    private readonly ILogger _logger;
    private readonly RoutedLink[] _routes;

    public MessageRoutes(RoutingConfig routingConfig, ILogger<MessageRoutes> logger)
    {
        _logger = logger;
        try
        {
            _routes = routingConfig.AllRoutes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating routing table");
            throw new RoutingException($"Error creating routing table: {ex.Message}", ex);
        }
    }

    public RoutingResult GetRoute(string routePath, SoapContent soapContent, string? correlationId, string? mrn)
    {
        string routeName;
        try
        {
            routeName = routePath.Trim('/');
            var route = _routes.FirstOrDefault(x =>
                x.RoutePath.Equals(routeName, StringComparison.InvariantCultureIgnoreCase)
                && soapContent.HasMessage(x.MessageSubXPath)
            );
            routePath = $"/{routeName.Trim('/')}";

            return route == null
                ? new RoutingResult
                {
                    RouteFound = false,
                    RouteName = null,
                    UrlPath = routePath,
                    StatusCode = HttpStatusCode.InternalServerError,
                    ErrorMessage = "Route not found",
                }
                : new RoutingResult
                {
                    RouteFound = true,
                    RouteName = route.Name,
                    MessageSubXPath = route.MessageSubXPath,
                    Legend = route.Legend,
                    FullRouteLink = route.BtmsLink,
                    RouteHostHeader = route.BtmsHostHeader,
                    ConvertRoutedContentToFromJson = true,
                    UrlPath = routePath,
                    StatusCode = HttpStatusCode.Accepted,
                    NamedProxy = route.NamedProxy,
                };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{ContentCorrelationId} {MRN} Error getting route", correlationId, mrn);
            return new RoutingResult
            {
                RouteFound = false,
                RouteName = routePath,
                UrlPath = routePath,
                StatusCode = HttpStatusCode.InternalServerError,
                ErrorMessage = $"Error getting route - {ex.Message} - {ex.InnerException?.Message}",
            };
        }
    }

    public bool IsCdsRoute(string routePath)
    {
        return _routes.Any(x =>
            x.IsCds && x.RoutePath.Equals(routePath.Trim('/'), StringComparison.InvariantCultureIgnoreCase)
        );
    }
}
