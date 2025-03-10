using System.Net;
using BtmsGateway.Middleware;
using BtmsGateway.Utils;
using BtmsGateway.Utils.Http;
using ILogger = Serilog.ILogger;

namespace BtmsGateway.Services.Routing;

public interface IMessageRouter
{
    Task<RoutingResult> Route(MessageData messageData, IMetrics metrics);
    Task<RoutingResult> Fork(MessageData messageData, IMetrics metrics);
}

public class MessageRouter(IMessageRoutes messageRoutes, IApiSender apiSender, IQueueSender queueSender, ILogger logger) : IMessageRouter
{
    public async Task<RoutingResult> Route(MessageData messageData, IMetrics metrics)
    {
        var routingResult = messageRoutes.GetRoute(messageData.Path);
        if (!routingResult.RouteFound || routingResult.RouteLinkType == LinkType.None) return routingResult;

        try
        {
            switch (routingResult.RouteLinkType)
            {
                case LinkType.Queue:
                    return await queueSender.Send(routingResult, messageData, metrics);
                case LinkType.Url:
                    return await apiSender.Send(routingResult, messageData, metrics);
                default:
                    return routingResult;
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error routing");
            return routingResult with { StatusCode = HttpStatusCode.ServiceUnavailable, ErrorMessage = $"Error routing - {ex.Message} - {ex.InnerException?.Message}" };
        }
    }

    public async Task<RoutingResult> Fork(MessageData messageData, IMetrics metrics)
    {
        var routingResult = messageRoutes.GetRoute(messageData.Path);
        if (!routingResult.RouteFound || routingResult.ForkLinkType == LinkType.None) return routingResult;

        try
        {
            switch (routingResult.ForkLinkType)
            {
                case LinkType.Queue:
                    return await queueSender.Send(routingResult, messageData, metrics, true);
                case LinkType.Url:
                    return await apiSender.Send(routingResult, messageData, metrics, true);
                default:
                    return routingResult;
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error forking");
            return routingResult with { StatusCode = HttpStatusCode.ServiceUnavailable, ErrorMessage = $"Error forking - {ex.Message} - {ex.InnerException?.Message}" };
        }
    }
}