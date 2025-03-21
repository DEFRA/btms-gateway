using System.Diagnostics.CodeAnalysis;
using System.Net;
using BtmsGateway.Services.Converter;
using ILogger = Serilog.ILogger;

namespace BtmsGateway.Services.Routing;

public interface IMessageRoutes
{
    RoutingResult GetRoute(string routePath, string? soapContent);
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
    public RoutingResult GetRoute(string routePath, string? soapContent)
    {
        string routeName;
        try
        {
            routeName = routePath.Trim('/');
            var route = _routes.FirstOrDefault(x => x.RoutePath.Equals(routeName, StringComparison.InvariantCultureIgnoreCase) && Soap.HasMessage(soapContent, x.MessageSubXPath));
            routePath = $"/{routeName.Trim('/')}";

            return route == null
                ? new RoutingResult { RouteFound = false, RouteName = null, UrlPath = routePath, StatusCode = HttpStatusCode.InternalServerError, ErrorMessage = "Route not found" }
                : route.RouteTo switch
                {
                    RouteTo.Legacy => new RoutingResult
                    {
                        RouteFound = true,
                        RouteName = route.Name,
                        Legend = route.Legend,
                        RouteLinkType = route.LegacyLinkType,
                        ForkLinkType = route.BtmsLinkType,
                        FullRouteLink = route.LegacyLinkType == LinkType.None ? null : $"{route.LegacyLink}{(route.LegacyLinkType == LinkType.Url ? routePath : null)}",
                        FullForkLink = route.BtmsLinkType == LinkType.None ? null : $"{route.BtmsLink}{(route.BtmsLinkType == LinkType.Url ? routePath : null)}",
                        RouteHostHeader = route.LegacyHostHeader,
                        ForkHostHeader = route.BtmsHostHeader,
                        MessageBodyDepth = route.MessageBodyDepth,
                        ConvertForkedContentToFromJson = true,
                        UrlPath = routePath
                    },
                    RouteTo.Btms => new RoutingResult
                    {
                        RouteFound = true,
                        RouteName = route.Name,
                        Legend = route.Legend,
                        RouteLinkType = route.BtmsLinkType,
                        ForkLinkType = route.LegacyLinkType,
                        FullRouteLink = route.BtmsLinkType == LinkType.None ? null : $"{route.BtmsLink}{(route.BtmsLinkType == LinkType.Url ? routePath : null)}",
                        FullForkLink = route.LegacyLinkType == LinkType.None ? null : $"{route.LegacyLink}{(route.LegacyLinkType == LinkType.Url ? routePath : null)}",
                        RouteHostHeader = route.BtmsHostHeader,
                        ForkHostHeader = route.LegacyHostHeader,
                        MessageBodyDepth = route.MessageBodyDepth,
                        ConvertRoutedContentToFromJson = true,
                        UrlPath = routePath
                    },
                    _ => throw new ArgumentOutOfRangeException(nameof(route.RouteTo), "Can only route to 'Legacy' or 'Btms'")
                };
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error getting route");
            return new RoutingResult { RouteFound = false, RouteName = routePath, UrlPath = routePath, StatusCode = HttpStatusCode.InternalServerError, ErrorMessage = $"Error getting route - {ex.Message} - {ex.InnerException?.Message}" };
        }
    }
}