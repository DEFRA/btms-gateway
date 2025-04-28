using System.Net;
using BtmsGateway.Domain;
using BtmsGateway.Middleware;
using BtmsGateway.Services.Metrics;
using ILogger = Serilog.ILogger;

namespace BtmsGateway.Services.Routing;

public interface IMessageRouter
{
    Task<RoutingResult> Route(MessageData messageData, IMetrics metrics);
    Task<RoutingResult> Fork(MessageData messageData, IMetrics metrics);
}

public class MessageRouter(
    IMessageRoutes messageRoutes,
    IApiSender apiSender,
    IQueueSender queueSender,
    ILogger logger,
    IDecisionSender decisionSender
) : IMessageRouter
{
    public async Task<RoutingResult> Route(MessageData messageData, IMetrics metrics)
    {
        var routingResult = messageRoutes.GetRoute(messageData.Path, messageData.OriginalSoapContent);
        if (!routingResult.RouteFound || routingResult.RouteLinkType == LinkType.None)
            return routingResult;

        try
        {
            metrics.StartRoutedRequest();

            routingResult = routingResult.RouteLinkType switch
            {
                LinkType.Queue => await queueSender.Send(routingResult, messageData, routingResult.FullRouteLink),
                LinkType.Url => await apiSender.Send(routingResult, messageData, fork: false),
                LinkType.DecisionComparer => await decisionSender.SendDecisionAsync(
                    messageData.ContentMap.EntryReference,
                    messageData.OriginalSoapContent.SoapString,
                    MessagingConstants.DecisionSource.Alvs,
                    messageData.Headers
                ),
                _ => routingResult,
            };

            return routingResult;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error routing");
            return routingResult with
            {
                StatusCode = HttpStatusCode.ServiceUnavailable,
                ErrorMessage = $"Error routing - {ex.Message} - {ex.InnerException?.Message}",
            };
        }
        finally
        {
            metrics.RecordRoutedRequest(routingResult);
        }
    }

    public async Task<RoutingResult> Fork(MessageData messageData, IMetrics metrics)
    {
        var routingResult = messageRoutes.GetRoute(messageData.Path, messageData.OriginalSoapContent);
        if (!routingResult.RouteFound || routingResult.ForkLinkType == LinkType.None)
            return routingResult;

        try
        {
            metrics.StartForkedRequest();

            routingResult = routingResult.ForkLinkType switch
            {
                LinkType.Queue => await queueSender.Send(routingResult, messageData, routingResult.FullForkLink),
                LinkType.Url => await apiSender.Send(routingResult, messageData, fork: true),
                LinkType.DecisionComparer => await decisionSender.SendDecisionAsync(
                    messageData.ContentMap.EntryReference,
                    messageData.OriginalSoapContent.SoapString,
                    MessagingConstants.DecisionSource.Alvs,
                    messageData.Headers
                ),
                _ => routingResult,
            };

            return routingResult;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error forking");
            return routingResult with
            {
                StatusCode = HttpStatusCode.ServiceUnavailable,
                ErrorMessage = $"Error forking - {ex.Message} - {ex.InnerException?.Message}",
            };
        }
        finally
        {
            metrics.RecordForkedRequest(routingResult);
        }
    }
}
