using System.Net;
using BtmsGateway.Middleware;
using BtmsGateway.Utils.Http;

namespace BtmsGateway.Services.Routing;

public interface IApiSender
{
    Task<RoutingResult> Send(RoutingResult routingResult, MessageData messageData, bool fork);
}

public class ApiSender(IHttpClientFactory clientFactory) : IApiSender
{
    public async Task<RoutingResult> Send(RoutingResult routingResult, MessageData messageData, bool fork)
    {
        var client = clientFactory.CreateClient(fork ? Proxy.ForkedClientWithRetry : Proxy.RoutedClientWithRetry);

        HttpRequestMessage request;

        if (fork)
        {
            request = routingResult.ConvertForkedContentToFromJson
                ? messageData.CreateConvertedForwardingRequest(routingResult.FullForkLink, routingResult.ForkHostHeader, routingResult.MessageSubXPath)
                : messageData.CreateForwardingRequestAsOriginal(routingResult.FullForkLink, routingResult.ForkHostHeader);
        }
        else
        {
            request = routingResult.ConvertRoutedContentToFromJson
                ? messageData.CreateConvertedForwardingRequest(routingResult.FullRouteLink, routingResult.RouteHostHeader, routingResult.MessageSubXPath)
                : messageData.CreateForwardingRequestAsOriginal(routingResult.FullRouteLink, routingResult.RouteHostHeader);
        }

        var response = await client.SendAsync(request);
        var content = response.StatusCode == HttpStatusCode.NoContent ? null : await response.Content.ReadAsStringAsync();

        return routingResult with
        {
            RoutingSuccessful = response.IsSuccessStatusCode,
            ResponseContent = content,
            StatusCode = response.StatusCode,
            ResponseDate = response.Headers.Date
        };
    }
}