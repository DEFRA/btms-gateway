using System.Net;
using BtmsGateway.Domain;
using BtmsGateway.Middleware;
using BtmsGateway.Services.Metrics;

namespace BtmsGateway.Services.Routing;

public interface IMessageRouter
{
    Task<RoutingResult> Route(MessageData messageData, IMetrics metrics);
}

public class MessageRouter(
    IMessageRoutes messageRoutes,
    IApiSender apiSender,
    IQueueSender queueSender,
    ILogger<MessageRouter> logger
) : IMessageRouter
{
    public async Task<RoutingResult> Route(MessageData messageData, IMetrics metrics)
    {
        var routingResult = GetRoutingResult(messageData);
        if (!routingResult.RouteFound || routingResult.RouteLinkType == LinkType.None)
            return routingResult;

        try
        {
            metrics.StartRoutedRequest();

            routingResult = routingResult.RouteLinkType switch
            {
                LinkType.Queue => await queueSender.Send(routingResult, messageData, routingResult.FullRouteLink),
                LinkType.Url => await apiSender.Send(routingResult, messageData),
                _ => routingResult,
            };

            return routingResult;
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "{ContentCorrelationId} {MessageReference} Error routing message type {MessageType}",
                messageData.ContentMap.CorrelationId,
                messageData.ContentMap.MessageReference,
                MessagingConstants.MessageTypes.FromSoapMessageType(routingResult.MessageSubXPath)
            );
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

    private RoutingResult GetRoutingResult(MessageData messageData)
    {
        return messageRoutes.GetRoute(
            messageData.Path,
            messageData.OriginalSoapContent,
            messageData.ContentMap.CorrelationId,
            messageData.ContentMap.MessageReference
        );
    }
}
