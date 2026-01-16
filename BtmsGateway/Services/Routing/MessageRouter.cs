using System.Net;
using BtmsGateway.Domain;
using BtmsGateway.Middleware;
using BtmsGateway.Services.Metrics;

namespace BtmsGateway.Services.Routing;

public interface IMessageRouter
{
    Task<RoutingResult> Route(MessageData messageData, IMetrics metrics);
}

public class MessageRouter(IMessageRoutes messageRoutes, IQueueSender queueSender, ILogger<MessageRouter> logger)
    : IMessageRouter
{
    public async Task<RoutingResult> Route(MessageData messageData, IMetrics metrics)
    {
        var routingResult = GetRoutingResult(messageData);
        if (!routingResult.RouteFound)
            return routingResult;

        try
        {
            metrics.StartRoutedRequest();
            return await queueSender.Send(routingResult, messageData, routingResult.FullRouteLink);
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
