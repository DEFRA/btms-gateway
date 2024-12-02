using BtmsGateway.Services.Routing;
using BtmsGateway.Utils;
using ILogger = Serilog.ILogger;

namespace BtmsGateway.Services;

public class SoapInterceptorMiddleware(RequestDelegate next, IMessageRouter messageRouter, IMessageFork messageFork, MetricsHost metricsHost, ILogger logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            var metrics = metricsHost.GetMetrics();
            metrics.StartTotalRequest();
            
            var messageData = await MessageData.Create(context.Request, logger);

            if (messageData.ShouldProcessRequest())
            {
                logger.Information("{CorrelationId} Received routing instruction {HttpString} {Content}", messageData.CorrelationId, messageData.HttpString, messageData.ContentAsString);

                await Route(context, messageData, metrics);

#pragma warning disable CS4014 // This call is not awaited as forking of the message should happen asynchronously
                await Fork(messageData, metrics);
#pragma warning restore CS4014
                
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
        const string Action = "Routing";
        var routingResult = await messageRouter.Route(messageData);

        if (routingResult.RouteFound)
        {
            CheckResults(messageData, routingResult, Action);
            if (routingResult.RoutingSuccessful) await messageData.PopulateResponse(context.Response, routingResult);
        }
        else
        {
            logger.Information("{CorrelationId} {Action} not supported for [{HttpString}]", messageData.CorrelationId, Action, messageData.HttpString);
        }

        metrics.RequestRouted(messageData, routingResult);
    }

    private async Task Fork(MessageData messageData, Metrics metrics)
    {
        const string Action = "Forking";
        var routingResult = await messageRouter.Fork(messageData);

        if (routingResult.RouteFound)
        {
            CheckResults(messageData, routingResult, Action);
            messageFork.Complete();
        }
        else
        {
            logger.Information("{CorrelationId} {Action} not supported for [{HttpString}]", messageData.CorrelationId, Action, messageData.HttpString);
        }
        
        metrics.RequestForked(messageData, routingResult);
    }

    private void CheckResults(MessageData messageData, RoutingResult routingResult, string action)
    {
        if (routingResult.RoutingSuccessful)
        {
            logger.Information("{CorrelationId} {Action} successful for route {RouteUrl} with response {StatusCode} {Content}", messageData.CorrelationId, action, routingResult.RouteUrl, routingResult.StatusCode, routingResult.ResponseContent);
        }
        else
        {
            logger.Information("{CorrelationId} {Action} failed for route {RouteUrl} with status code {StatusCode}", messageData.CorrelationId, action, routingResult.RouteUrl, routingResult.StatusCode);
        }
    }
}