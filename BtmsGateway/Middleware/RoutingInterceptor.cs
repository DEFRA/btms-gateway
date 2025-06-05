using BtmsGateway.Config;
using BtmsGateway.Exceptions;
using BtmsGateway.Services.Metrics;
using BtmsGateway.Services.Routing;
using Microsoft.Extensions.Options;
using ILogger = Serilog.ILogger;

namespace BtmsGateway.Middleware;

public class RoutingInterceptor(
    RequestDelegate next,
    IMessageRouter messageRouter,
    MetricsHost metricsHost,
    IRequestMetrics requestMetrics,
    ILogger logger,
    IOptions<MessageLoggingOptions> messageLoggingOptions
)
{
    private const string RouteAction = "Routing";
    private const string ForkAction = "Forking";

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            var metrics = metricsHost.GetMetrics();

            var messageData = await MessageData.Create(
                context.Request,
                logger,
                messageLoggingOptions.Value.LogRawMessage
            );

            if (messageData.ShouldProcessRequest)
            {
                logger.Information(
                    "{ContentCorrelationId} {MessageReference} Received routing instruction {HttpString}",
                    messageData.ContentMap.CorrelationId,
                    messageData.ContentMap.MessageReference,
                    messageData.HttpString
                );

                await Route(context, messageData, metrics);

                await Fork(messageData, metrics);

                return;
            }

            logger.Information(
                "{ContentCorrelationId} Pass through request {HttpString}",
                messageData.ContentMap.CorrelationId,
                messageData.HttpString
            );

            await next(context);
        }
        catch (Exception ex)
        {
            logger.Error(ex, "There was a routing error");
            throw new RoutingException($"There was a routing error: {ex.Message}", ex);
        }
    }

    private async Task Route(HttpContext context, MessageData messageData, IMetrics metrics)
    {
        var routingResult = await messageRouter.Route(messageData, metrics);

        RecordRequest(routingResult, RouteAction);

        if (routingResult.RouteFound && routingResult.RouteLinkType != LinkType.None)
            LogRouteFoundResults(messageData, routingResult, RouteAction);
        else
            LogRouteNotFoundResults(messageData, routingResult, RouteAction);

        await messageData.PopulateResponse(context.Response, routingResult);
    }

    private async Task Fork(MessageData messageData, IMetrics metrics)
    {
        var routingResult = await messageRouter.Fork(messageData, metrics);

        RecordRequest(routingResult, ForkAction);

        if (routingResult.RouteFound && routingResult.ForkLinkType != LinkType.None)
            LogRouteFoundResults(messageData, routingResult, ForkAction);
        else
            LogRouteNotFoundResults(messageData, routingResult, ForkAction);
    }

    private void RecordRequest(RoutingResult routingResult, string action)
    {
        if (routingResult.RouteFound)
        {
            requestMetrics.MessageReceived(
                routingResult.MessageSubXPath,
                routingResult.UrlPath,
                routingResult.Legend,
                action
            );
        }
    }

    private void LogRouteFoundResults(MessageData messageData, RoutingResult routingResult, string action)
    {
        if (routingResult.RoutingSuccessful)
        {
            logger.Information(
                "{ContentCorrelationId} {MessageReference} {Action} {Success} for route {RouteUrl} with response {StatusCode} \"{Content}\"",
                messageData.ContentMap.CorrelationId,
                messageData.ContentMap.MessageReference,
                action,
                "successful",
                action == RouteAction ? routingResult.FullRouteLink : routingResult.FullForkLink,
                routingResult.StatusCode,
                routingResult.ResponseContent
            );
            return;
        }

        logger.Error(
            "{ContentCorrelationId} {MessageReference} {Action} {Success} for route {RouteUrl} with response {StatusCode} \"{Content}\"",
            messageData.ContentMap.CorrelationId,
            messageData.ContentMap.MessageReference,
            action,
            "failed",
            action == RouteAction ? routingResult.FullRouteLink : routingResult.FullForkLink,
            routingResult.StatusCode,
            routingResult.ResponseContent
        );
    }

    private void LogRouteNotFoundResults(MessageData messageData, RoutingResult routingResult, string action)
    {
        logger.Warning(
            "{ContentCorrelationId} {MessageReference} {Action} not {Reason} for [{HttpString}]",
            messageData.ContentMap.CorrelationId,
            messageData.ContentMap.MessageReference,
            action,
            GetReason(action == RouteAction ? routingResult.RouteLinkType : routingResult.ForkLinkType),
            messageData.HttpString
        );
    }

    private static string GetReason(LinkType linkType)
    {
        return linkType == LinkType.None ? "configured" : "supported";
    }
}
