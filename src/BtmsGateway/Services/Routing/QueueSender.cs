using System.Net;
using Amazon.SimpleNotificationService;
using BtmsGateway.Exceptions;
using BtmsGateway.Middleware;
using BtmsGateway.Services.Converter;
using BtmsGateway.Utils;

namespace BtmsGateway.Services.Routing;

public interface IQueueSender
{
    Task<RoutingResult> Send(RoutingResult routingResult, MessageData messageData, string? route);
}

public class QueueSender(
    IAmazonSimpleNotificationService snsService,
    IConfiguration configuration,
    ILogger<QueueSender> logger
) : IQueueSender
{
    public async Task<RoutingResult> Send(RoutingResult routingResult, MessageData messageData, string? route)
    {
        try
        {
            var traceIdHeader = configuration.GetValue<string>("TraceHeader");

            var request = messageData.CreatePublishRequest(route, routingResult.MessageSubXPath, traceIdHeader);

            var response = await snsService.PublishAsync(request);

            if (response.HttpStatusCode.IsSuccessStatusCode())
            {
                logger.LogInformation(
                    "{ContentCorrelationId} {MessageReference} Successfully published MessageId: {MessageId}",
                    messageData.ContentMap.CorrelationId,
                    messageData.ContentMap.MessageReference,
                    response.MessageId
                );

                return routingResult with
                {
                    RoutingSuccessful = true,
                    ResponseContent = SoapUtils.GetMessageTypeSuccessResponse(routingResult.MessageSubXPath),
                    StatusCode = response.HttpStatusCode,
                };
            }

            logger.LogError(
                "{ContentCorrelationId} {MessageReference} Failed to publish message to inbound topic",
                messageData.ContentMap.CorrelationId,
                messageData.ContentMap.MessageReference
            );

            return RoutingResultWithStatusCode(routingResult, response.HttpStatusCode);
        }
        catch (InvalidSoapException ex)
        {
            logger.LogWarning(
                ex,
                "{ContentCorrelationId} {MessageReference} Invalid SOAP message",
                messageData.ContentMap.CorrelationId,
                messageData.ContentMap.MessageReference
            );
            return RoutingResultWithStatusCode(routingResult, HttpStatusCode.BadRequest);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "{ContentCorrelationId} {MessageReference} Failed to publish message to inbound topic",
                messageData.ContentMap.CorrelationId,
                messageData.ContentMap.MessageReference
            );

            return RoutingResultWithStatusCode(routingResult, HttpStatusCode.InternalServerError);
        }
    }

    private static RoutingResult RoutingResultWithStatusCode(RoutingResult routingResult, HttpStatusCode statusCode)
    {
        return routingResult with
        {
            RoutingSuccessful = false,
            ResponseContent = SoapUtils.FailedSoapRequestResponseBody,
            StatusCode = statusCode,
        };
    }
}
