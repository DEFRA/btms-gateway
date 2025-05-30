using Amazon.SimpleNotificationService;
using BtmsGateway.Middleware;
using BtmsGateway.Utils;

namespace BtmsGateway.Services.Routing;

public interface IQueueSender
{
    Task<RoutingResult> Send(RoutingResult routingResult, MessageData messageData, string? route);
}

public class QueueSender(IAmazonSimpleNotificationService snsService, IConfiguration configuration) : IQueueSender
{
    public async Task<RoutingResult> Send(RoutingResult routingResult, MessageData messageData, string? route)
    {
        var traceIdHeader = configuration.GetValue<string>("TraceHeader");
        var request = messageData.CreatePublishRequest(route, routingResult.MessageSubXPath, traceIdHeader);

        var response = await snsService.PublishAsync(request);

        return routingResult with
        {
            RoutingSuccessful = response.HttpStatusCode.IsSuccessStatusCode(),
            ResponseContent = $"Successfully published MessageId: {response.MessageId}",
            StatusCode = response.HttpStatusCode,
        };
    }
}
