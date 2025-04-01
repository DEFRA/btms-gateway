using BtmsGateway.Services.Metrics;
using BtmsGateway.Services.Routing;
using ILogger = Serilog.ILogger;

namespace BtmsGateway.Middleware;

public class RoutingInterceptor(RequestDelegate next, IMessageRouter messageRouter, MetricsHost metricsHost, ILogger logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            var metrics = metricsHost.GetMetrics();

            var messageData = await MessageData.Create(context.Request, logger);

            if (messageData.ShouldProcessRequest)
            {
                logger.Information("{ContentCorrelationId} Received routing instruction {HttpString} {Content}", messageData.ContentMap.CorrelationId, messageData.HttpString, messageData.OriginalSoapContent.SoapString);

                await Route(context, messageData, metrics);

                await Fork(messageData, metrics);

                return;
            }

            logger.Information("{ContentCorrelationId} Pass through request {HttpString}", messageData.ContentMap.CorrelationId, messageData.HttpString);

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
    }

    private async Task Fork(MessageData messageData, IMetrics metrics)
    {
        var routingResult = await messageRouter.Fork(messageData, metrics);

        if (routingResult.RouteFound && routingResult.ForkLinkType != LinkType.None)
            LogRouteFoundResults(messageData, routingResult, "Forking");
        else
            LogRouteNotFoundResults(messageData, routingResult, "Forking");
    }

    private void LogRouteFoundResults(MessageData messageData, RoutingResult routingResult, string action)
    {
        logger.Information("{ContentCorrelationId} {Action} {Success} for route {RouteUrl} with response {StatusCode} \"{Content}\"", messageData.ContentMap.CorrelationId, action, routingResult.RoutingSuccessful ? "successful" : "failed", routingResult.FullRouteLink, routingResult.StatusCode, routingResult.ResponseContent);
    }

    private void LogRouteNotFoundResults(MessageData messageData, RoutingResult routingResult, string action)
    {
        logger.Information("{ContentCorrelationId} {Action} not {Reason} for [{HttpString}]", messageData.ContentMap.CorrelationId, action, routingResult.RouteLinkType == LinkType.None ? "configured" : "supported", messageData.HttpString);
    }
}