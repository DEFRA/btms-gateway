using System.Net;
using BtmsGateway.Middleware;
using BtmsGateway.Utils;
using BtmsGateway.Utils.Http;

namespace BtmsGateway.Services.Routing;

public interface IApiSender
{
    Task<RoutingResult> Send(RoutingResult routingResult, MessageData messageData, IMetrics metrics, bool fork = false);
}

public class ApiSender(IHttpClientFactory clientFactory) : IApiSender
{
    public async Task<RoutingResult> Send(RoutingResult routingResult, MessageData messageData, IMetrics metrics, bool fork = false)
    {
        var client = clientFactory.CreateClient(fork ? Proxy.ForkedClientWithRetry : Proxy.RoutedClientWithRetry);

        HttpRequestMessage request;

        if (fork)
        {
            request = routingResult.ConvertForkedContentToFromJson
                ? messageData.CreateConvertedForwardingRequest(routingResult.FullForkLink, routingResult.ForkHostHeader, routingResult.MessageBodyDepth)
                : messageData.CreateForwardingRequestAsOriginal(routingResult.FullForkLink, routingResult.ForkHostHeader);
            metrics.StartForkedRequest();
        }
        else
        {
            request = routingResult.ConvertRoutedContentToFromJson
                ? messageData.CreateConvertedForwardingRequest(routingResult.FullRouteLink, routingResult.RouteHostHeader, routingResult.MessageBodyDepth)
                : messageData.CreateForwardingRequestAsOriginal(routingResult.FullRouteLink, routingResult.RouteHostHeader);
            metrics.StartRoutedRequest();
        }

        var response = await client.SendAsync(request);
        var content = response.StatusCode == HttpStatusCode.NoContent ? null : await response.Content.ReadAsStringAsync();

        if (fork)
            metrics.RecordForkedRequest(messageData, routingResult);
        else
            metrics.RecordRoutedRequest(messageData, routingResult);

        return routingResult with
        {
            RoutingSuccessful = response.IsSuccessStatusCode,
            ResponseContent = content,
            StatusCode = response.StatusCode,
            ResponseDate = response.Headers.Date
        };
    }
}