using System.Net;
using System.Text;
using BtmsGateway.Config;
using BtmsGateway.Domain;
using BtmsGateway.Exceptions;
using BtmsGateway.Services.Converter;
using BtmsGateway.Services.Metrics;
using BtmsGateway.Services.Routing;
using Microsoft.Extensions.Options;

namespace BtmsGateway.Middleware;

public class RoutingInterceptor(
    RequestDelegate next,
    IMessageRouter messageRouter,
    MetricsHost metricsHost,
    IRequestMetrics requestMetrics,
    ILogger<RoutingInterceptor> logger,
    IOptions<MessageLoggingOptions> messageLoggingOptions,
    IMessageRoutes messageRoutes
)
{
    private const string RouteAction = "Routing";

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
                logger.LogInformation(
                    "{ContentCorrelationId} {MessageReference} version {Version} Received routing instruction {HttpString}",
                    messageData.ContentMap.CorrelationId,
                    messageData.ContentMap.MessageReference,
                    messageData.ContentMap.EntryVersionNumber,
                    messageData.HttpString
                );

                await Route(context, messageData, metrics);
                return;
            }

            logger.LogInformation(
                "{ContentCorrelationId} Pass through request {HttpString}",
                messageData.ContentMap.CorrelationId,
                messageData.HttpString
            );

            await next(context);
        }
        catch (InvalidSoapException ex)
        {
            if (messageRoutes.IsCdsRoute(context.Request.Path))
            {
                // Log as Warning as we won't do anything with it at this point, and we don't want additional errors causing potential alerts.
                // The upstream system needs to sort out the invalid request
                logger.LogWarning(ex, "Invalid SOAP Message");
                await PopulateInvalidSoapResponse(context);
                throw new RoutingException("Invalid SOAP Message", ex);
            }

            ReturnRoutingError(ex, context);
        }
        catch (Exception ex)
        {
            ReturnRoutingError(ex, context);
        }
    }

    private void ReturnRoutingError(Exception ex, HttpContext context)
    {
        logger.LogError(ex, "There was a routing error");
        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
        throw new RoutingException($"There was a routing error: {ex.Message}", ex);
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

        if (routingResult.RouteFound)
            LogRouteFoundResults(messageData, routingResult, RouteAction);
        else
            LogRouteNotFoundResults(messageData, routingResult, RouteAction);

        await messageData.PopulateResponse(context.Response, routingResult);
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
            logger.LogInformation(
                "{ContentCorrelationId} {MessageReference} {Version} {Action} {Success} for route {RouteUrl}, message type {MessageType} with response {StatusCode}",
                messageData.ContentMap.CorrelationId,
                messageData.ContentMap.MessageReference,
                messageData.ContentMap.EntryVersionNumber,
                action,
                "successful",
                routingResult.FullRouteLink,
                MessagingConstants.MessageTypes.FromSoapMessageType(routingResult.MessageSubXPath),
                routingResult.StatusCode
            );

            requestMetrics.MessageSuccessfullySent(
                routingResult.MessageSubXPath,
                routingResult.UrlPath,
                routingResult.Legend,
                action
            );

            return;
        }

        logger.LogError(
            "{ContentCorrelationId} {MessageReference} {Version} {Action} {Success} for route {RouteUrl}, message type {MessageType} with response {StatusCode}",
            messageData.ContentMap.CorrelationId,
            messageData.ContentMap.MessageReference,
            messageData.ContentMap.EntryVersionNumber,
            action,
            "failed",
            routingResult.FullRouteLink,
            MessagingConstants.MessageTypes.FromSoapMessageType(routingResult.MessageSubXPath),
            routingResult.StatusCode
        );
    }

    private void LogRouteNotFoundResults(MessageData messageData, RoutingResult routingResult, string action)
    {
        if (!routingResult.RouteFound)
        {
            logger.LogWarning(
                "{ContentCorrelationId} {MessageReference} {Version} {Action} not supported for [{HttpString}]",
                messageData.ContentMap.CorrelationId,
                messageData.ContentMap.MessageReference,
                messageData.ContentMap.EntryVersionNumber,
                action,
                messageData.HttpString
            );
        }
    }
}
