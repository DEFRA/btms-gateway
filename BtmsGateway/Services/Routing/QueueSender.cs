using System.Net;
using Amazon.SimpleNotificationService;
using BtmsGateway.Middleware;
using BtmsGateway.Utils;

namespace BtmsGateway.Services.Routing;

public interface IQueueSender
{
    Task<RoutingResult> Send(RoutingResult routingResult, MessageData messageData, IMetrics metrics, bool fork = false);
}

public class QueueSender(IAmazonSimpleNotificationService snsService) : IQueueSender
{
    public async Task<RoutingResult> Send(RoutingResult routingResult, MessageData messageData, IMetrics metrics, bool fork = false)
    {
        var route = fork ? routingResult.FullForkLink : routingResult.FullRouteLink;
        var request = messageData.CreatePublishRequest(route, routingResult.MessageBodyDepth);

        if (fork)
            metrics.StartForkedRequest();
        else
            metrics.StartRoutedRequest();

        var response = await snsService.PublishAsync(request);

        if (fork)
            metrics.RecordForkedRequest(messageData, routingResult);
        else
            metrics.RecordRoutedRequest(messageData, routingResult);

        return routingResult
         with
        {
            RoutingSuccessful = response.HttpStatusCode == HttpStatusCode.OK,
            ResponseContent = $"Successfully published MessageId: {response.MessageId}",
            StatusCode = response.HttpStatusCode
        };
    }
}