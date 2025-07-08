using System.Net;
using System.Text;
using BtmsGateway.Config;
using BtmsGateway.Domain;
using BtmsGateway.Exceptions;
using BtmsGateway.Services.Converter;
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
    IOptions<MessageLoggingOptions> messageLoggingOptions,
    IMessageRoutes messageRoutes
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
        catch (InvalidSoapException ex)
        {
            logger.Warning(ex, "Invalid SOAP Message");

            if (messageRoutes.IsCdsRoute(context.Request.Path))
            {
                await PopulateInvalidSoapResponse(context);
            }
            else
            {
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            }

            throw new RoutingException($"Invalid SOAP Message: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            logger.Error(ex, "There was a routing error");
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            throw new RoutingException($"There was a routing error: {ex.Message}", ex);
        }
    }

    private static async Task PopulateInvalidSoapResponse(HttpContext context)
    {
        context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
        context.Response.ContentType = "application/soap+xml";
        context.Response.Headers.Date = DateTimeOffset.Now.ToString("R");
        context.Response.Headers["x-requested-path"] = context.Request.Path.HasValue
            ? $"/{context.Request.Path.Value.Trim('/')}"
            : string.Empty;
        await context.Response.BodyWriter.WriteAsync(
            new ReadOnlyMemory<byte>(Encoding.UTF8.GetBytes(SoapUtils.FailedSoapRequestResponseBody))
        );
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
                "{ContentCorrelationId} {MessageReference} {Action} {Success} for route {RouteUrl}, message type {MessageType} with response {StatusCode} \"{Content}\"",
                messageData.ContentMap.CorrelationId,
                messageData.ContentMap.MessageReference,
                action,
                "successful",
                action == RouteAction ? routingResult.FullRouteLink : routingResult.FullForkLink,
                MessagingConstants.MessageTypes.FromSoapMessageType(routingResult.MessageSubXPath),
                routingResult.StatusCode,
                routingResult.ResponseContent
            );

            requestMetrics.MessageSuccessfullySent(
                routingResult.MessageSubXPath,
                routingResult.UrlPath,
                routingResult.Legend,
                action
            );

            return;
        }

        logger.Error(
            "{ContentCorrelationId} {MessageReference} {Action} {Success} for route {RouteUrl}, message type {MessageType} with response {StatusCode} \"{Content}\"",
            messageData.ContentMap.CorrelationId,
            messageData.ContentMap.MessageReference,
            action,
            "failed",
            action == RouteAction ? routingResult.FullRouteLink : routingResult.FullForkLink,
            MessagingConstants.MessageTypes.FromSoapMessageType(routingResult.MessageSubXPath),
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
