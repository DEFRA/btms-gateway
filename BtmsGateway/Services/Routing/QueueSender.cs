using System.Net;
using Amazon.SimpleNotificationService;
using BtmsGateway.Middleware;

namespace BtmsGateway.Services.Routing;

public interface IQueueSender
{
    Task<RoutingResult> Send(RoutingResult routingResult, MessageData messageData, string? route);
}

public class QueueSender(IAmazonSimpleNotificationService snsService) : IQueueSender
{
    public async Task<RoutingResult> Send(RoutingResult routingResult, MessageData messageData, string? route)
    {
        var request = messageData.CreatePublishRequest(route, routingResult.MessageBodyDepth);

        var response = await snsService.PublishAsync(request);

        return routingResult with
        {
            RoutingSuccessful = response.HttpStatusCode == HttpStatusCode.OK,
            ResponseContent = $"Successfully published MessageId: {response.MessageId}",
            StatusCode = response.HttpStatusCode
        };
    }
}