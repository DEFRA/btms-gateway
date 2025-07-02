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
    IDecisionSender decisionSender,
    IErrorNotificationSender errorNotificationSender
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
                LinkType.Url => await apiSender.Send(routingResult, messageData, fork: false),
                LinkType.DecisionComparer => await decisionSender.SendDecisionAsync(
                    messageData.ContentMap.EntryReference,
                    messageData.OriginalSoapContent.RawSoapString,
                    MessagingConstants.MessageSource.Alvs,
                    routingResult,
                    messageData.Headers,
                    messageData.ContentMap.CorrelationId
                ),
                LinkType.DecisionComparerErrorNotifications => await errorNotificationSender.SendErrorNotificationAsync(
                    messageData.ContentMap.EntryReference,
                    messageData.OriginalSoapContent.RawSoapString,
                    MessagingConstants.MessageSource.Alvs,
                    routingResult,
                    messageData.Headers,
                    messageData.ContentMap.CorrelationId
                ),
                _ => routingResult,
            };

            return routingResult;
        }
        catch (Exception ex)
        {
            LogRoutingError(ex, messageData, routingResult);
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
        var routingResult = GetRoutingResult(messageData);
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
                    messageData.OriginalSoapContent.RawSoapString,
                    MessagingConstants.MessageSource.Alvs,
                    routingResult,
                    messageData.Headers,
                    messageData.ContentMap.CorrelationId
                ),
                LinkType.DecisionComparerErrorNotifications => await errorNotificationSender.SendErrorNotificationAsync(
                    messageData.ContentMap.EntryReference,
                    messageData.OriginalSoapContent.RawSoapString,
                    MessagingConstants.MessageSource.Alvs,
                    routingResult,
                    messageData.Headers,
                    messageData.ContentMap.CorrelationId
                ),
                _ => routingResult,
            };

            return routingResult;
        }
        catch (Exception ex)
        {
            LogForkingError(ex, messageData, routingResult);
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

    private void LogRoutingError(Exception? ex, MessageData messageData, RoutingResult routingResult)
    {
        LogError(ex, messageData, "routing", routingResult);
    }

    private void LogForkingError(Exception? ex, MessageData messageData, RoutingResult routingResult)
    {
        LogError(ex, messageData, "forking", routingResult);
    }

    private void LogError(Exception? ex, MessageData messageData, string action, RoutingResult routingResult)
    {
        logger.Error(
            ex,
            "{ContentCorrelationId} {MessageReference} Error {Action} message type {MessageType}",
            messageData.ContentMap.CorrelationId,
            messageData.ContentMap.MessageReference,
            action,
            MessagingConstants.MessageTypes.FromSoapMessageType(routingResult.MessageSubXPath)
        );
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
