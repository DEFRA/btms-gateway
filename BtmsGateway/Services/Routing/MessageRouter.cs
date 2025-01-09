using System.Net;
using BtmsGateway.Utils;
using BtmsGateway.Utils.Http;
using ILogger = Serilog.ILogger;

namespace BtmsGateway.Services.Routing;

public interface IMessageRouter
{
    Task<RoutingResult> Route(MessageData messageData);
    Task<RoutingResult> Fork(MessageData messageData);
}

public class MessageRouter(IHttpClientFactory clientFactory, IMessageRoutes messageRoutes, MetricsHost metricsHost, ILogger logger) : IMessageRouter
{
    public async Task<RoutingResult> Route(MessageData messageData)
    {
        var routingResult = messageRoutes.GetRoute(messageData.Path);
        if (!routingResult.RouteFound) return routingResult;
        
        try
        {
            var metrics = metricsHost.GetMetrics();
            var client = clientFactory.CreateClient(Proxy.RoutedClientWithRetry);
            var request = routingResult.ConvertedRoutedContentToJson 
                ? messageData.CreateForwardingRequestAsJson(routingResult.FullRouteLink) 
                : messageData.CreateForwardingRequestAsOriginal(routingResult.FullRouteLink);
            
            metrics.StartRoutedRequest();
            var response = await client.SendAsync(request);
            var content = response.StatusCode == HttpStatusCode.NoContent ? null : await response.Content.ReadAsStringAsync();
            metrics.RecordRoutedRequest();
            
            return routingResult with { RoutingSuccessful = response.IsSuccessStatusCode, ResponseContent = content, StatusCode = response.StatusCode, ResponseDate = response.Headers.Date };
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error routing");
            return routingResult with { StatusCode = HttpStatusCode.ServiceUnavailable };
        }
    }
    
    public async Task<RoutingResult> Fork(MessageData messageData)
    {
        var routingResult = messageRoutes.GetRoute(messageData.Path);
        if (!routingResult.RouteFound) return routingResult;
        
        try
        {
            var metrics = metricsHost.GetMetrics();
            var client = clientFactory.CreateClient(Proxy.ForkedClientWithRetry);
            var request = routingResult.ConvertedForkedContentToJson 
                ? messageData.CreateForwardingRequestAsJson(routingResult.FullForkLink) 
                : messageData.CreateForwardingRequestAsOriginal(routingResult.FullForkLink);
            
            metrics.StartForkedRequest();
            var response = await client.SendAsync(request);
            var content = response.StatusCode == HttpStatusCode.NoContent ? null : await response.Content.ReadAsStringAsync();
            metrics.RecordForkedRequest();
            
            return routingResult with { RoutingSuccessful = response.IsSuccessStatusCode, ResponseContent = content, StatusCode = response.StatusCode, ResponseDate = response.Headers.Date };
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error forking");
            return routingResult with { StatusCode = HttpStatusCode.ServiceUnavailable };
        }
    }
}