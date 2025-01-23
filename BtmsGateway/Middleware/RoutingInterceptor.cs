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

    private async Task Route(HttpContext context, MessageData messageData, Metrics metrics)
    {
        var routingResult = await messageRouter.Route(messageData);

        if (routingResult.RouteFound && routingResult.RouteLinkType != LinkType.None)
        {
            CheckResults(messageData, routingResult, "Routing");
        }
        else if (routingResult.RouteLinkType == LinkType.None)
        {
            logger.Information("{CorrelationId} Routing not configured for [{HttpString}]", messageData.CorrelationId, messageData.HttpString);
        }
        else
        {
            logger.Information("{CorrelationId} Routing not supported for [{HttpString}]", messageData.CorrelationId, messageData.HttpString);
        }

        await messageData.PopulateResponse(context.Response, routingResult);

        metrics.RequestRouted(messageData, routingResult);
    }

    private async Task Fork(MessageData messageData, Metrics metrics)
    {
        var routingResult = await messageRouter.Fork(messageData);

        if (routingResult.RouteFound && routingResult.ForkLinkType != LinkType.None)
        {
            CheckResults(messageData, routingResult, "Forking");
        }
        else if (routingResult.ForkLinkType == LinkType.None)
        {
            logger.Information("{CorrelationId} Forking not configured for [{HttpString}]", messageData.CorrelationId, messageData.HttpString);
        }
        else
        {
            logger.Information("{CorrelationId} Forking not supported for [{HttpString}]", messageData.CorrelationId, messageData.HttpString);
        }
        
        metrics.RequestForked(messageData, routingResult);
    }

    private void CheckResults(MessageData messageData, RoutingResult routingResult, string action)
    {
        if (routingResult.RoutingSuccessful)
        {
            logger.Information("{CorrelationId} {Action} successful for route {RouteUrl} with response {StatusCode} {Content}", messageData.CorrelationId, action, routingResult.FullRouteLink, routingResult.StatusCode, routingResult.ResponseContent);
        }
        else
        {
            logger.Information("{CorrelationId} {Action} failed for route {RouteUrl} with status code {StatusCode}", messageData.CorrelationId, action, routingResult.FullRouteLink, routingResult.StatusCode);
        }
    }
}