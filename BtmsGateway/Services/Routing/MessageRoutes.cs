using System.Diagnostics.CodeAnalysis;
using System.Net;
using BtmsGateway.Domain;
using BtmsGateway.Exceptions;
using BtmsGateway.Services.Converter;
using ILogger = Serilog.ILogger;

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

    public MessageRoutes(RoutingConfig routingConfig, ILogger logger)
    {
        _logger = logger;
        try
        {
            if (
                routingConfig.NamedLinks.Any(x =>
                    x.Value.LinkType == LinkType.Url && !Uri.TryCreate(x.Value.Link, UriKind.Absolute, out _)
                )
            )
                throw new InvalidDataException("Invalid URL(s) in config");
            if (routingConfig.NamedRoutes.Any(x => !Enum.IsDefined(x.Value.RouteTo)))
                throw new InvalidDataException("Invalid Route To in config");
            if (routingConfig.NamedLinks.Any(x => !Enum.IsDefined(x.Value.LinkType)))
                throw new InvalidDataException("Invalid Link Type in config");
            _routes = routingConfig.AllRoutes;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error creating routing table");
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
                : SelectRoute(route, routePath);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "{ContentCorrelationId} {MRN} Error getting route", correlationId, mrn);
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
        var route = _routes.FirstOrDefault(x =>
            x.RoutePath.Equals(routePath.Trim('/'), StringComparison.InvariantCultureIgnoreCase)
        );

        return MessagingConstants.Routes.CdsRoutes.Contains(route?.Name);
    }

    private static RoutingResult SelectRoute(RoutedLink route, string routePath)
    {
        return route.RouteTo switch
        {
            RouteTo.Legacy => new RoutingResult
            {
                RouteFound = true,
                RouteName = route.Name,
                MessageSubXPath = route.MessageSubXPath,
                Legend = route.Legend,
                RouteLinkType = route.LegacyLinkType,
                ForkLinkType = route.BtmsLinkType,
                FullRouteLink = SelectLink(route.LegacyLinkType, route.LegacyLink, routePath),
                FullForkLink = SelectLink(route.BtmsLinkType, route.BtmsLink, routePath),
                RouteHostHeader = route.LegacyHostHeader,
                ForkHostHeader = route.BtmsHostHeader,
                ConvertForkedContentToFromJson = true,
                UrlPath = routePath,
            },
            RouteTo.Btms => new RoutingResult
            {
                RouteFound = true,
                RouteName = route.Name,
                MessageSubXPath = route.MessageSubXPath,
                Legend = route.Legend,
                RouteLinkType = route.BtmsLinkType,
                ForkLinkType = route.LegacyLinkType,
                FullRouteLink = SelectLink(route.BtmsLinkType, route.BtmsLink, routePath),
                FullForkLink = SelectLink(route.LegacyLinkType, route.LegacyLink, routePath),
                RouteHostHeader = route.BtmsHostHeader,
                ForkHostHeader = route.LegacyHostHeader,
                ConvertRoutedContentToFromJson = true,
                UrlPath = routePath,
            },
            _ => throw new ArgumentOutOfRangeException(
                nameof(route),
                "Invalid RouteTo property value specified in route argument. Can only route to 'Legacy' or 'Btms'"
            ),
        };
    }

    [SuppressMessage(
        "SonarLint",
        "S3358",
        Justification = "The second nested ternary in each case (lines 55, 56, 68, 69) is within a string interpolation so is very clearly independent of the first"
    )]
    private static string? SelectLink(LinkType linkType, string? link, string routePath)
    {
        return linkType == LinkType.None ? null : $"{link}{(linkType == LinkType.Url ? routePath : null)}";
    }
}
