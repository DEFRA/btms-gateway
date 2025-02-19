using BtmsGateway.Services.Routing;
using BtmsGateway.Utils;
using ILogger = Serilog.ILogger;

namespace BtmsGateway.Middleware;

public class RoutingInterceptor(RequestDelegate next, IMessageRouter messageRouter, MetricsHost metricsHost, ILogger logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            var metrics = metricsHost.GetMetrics();
            metrics.StartTotalRequest();
            
            var messageData = await MessageData.Create(context.Request, logger);

            if (messageData.ShouldProcessRequest)
            {
                logger.Information("{CorrelationId} Received routing instruction {HttpString} {Content}", messageData.CorrelationId, messageData.HttpString, messageData.OriginalContentAsString);

                await Route(context, messageData, metrics);

                await Fork(messageData, metrics);
                
                metrics.RecordTotalRequest();
                return;
            }

            logger.Information("{CorrelationId} Pass through request {HttpString}", messageData.CorrelationId, messageData.HttpString);

            await next(context);
        }
        catch (Exception ex)
        {
            logger.Error(ex, "There was a routing error");
            throw;
        }
    }

    private async Task Route(HttpContext context, MessageData messageData, IMetrics metrics)
    {
        var routingResult = await messageRouter.Route(messageData, metrics);

        if (routingResult.RouteFound && routingResult.RouteLinkType != LinkType.None)
            LogRouteFoundResults(messageData, routingResult, "Routing");
        else
            LogRouteNotFoundResults(messageData, routingResult, "Routing");

        await messageData.PopulateResponse(context.Response, routingResult);

        metrics.RequestRouted(messageData, routingResult);
    }

    private async Task Fork(MessageData messageData, IMetrics metrics)
    {
        var routingResult = await messageRouter.Fork(messageData, metrics);

        if (routingResult.RouteFound && routingResult.ForkLinkType != LinkType.None)
            LogRouteFoundResults(messageData, routingResult, "Forking");
        else
            LogRouteNotFoundResults(messageData, routingResult, "Forking");
        
        metrics.RequestForked(messageData, routingResult);
    }

    private void LogRouteFoundResults(MessageData messageData, RoutingResult routingResult, string action)
    {
        logger.Information("{CorrelationId} {Action} {Success} for route {RouteUrl} with response {StatusCode} \"{Content}\"", messageData.CorrelationId, action, routingResult.RoutingSuccessful ? "successful" : "failed", routingResult.FullRouteLink, routingResult.StatusCode, routingResult.ResponseContent);
    }

    private void LogRouteNotFoundResults(MessageData messageData, RoutingResult routingResult, string action)
    {
        logger.Information("{CorrelationId} {Action} not {Reason} for [{HttpString}]", messageData.CorrelationId, action, routingResult.RouteLinkType == LinkType.None ? "configured" : "supported", messageData.HttpString);
    }
}