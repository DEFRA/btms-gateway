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
        var routingResult = messageRoutes.GetRoutedRoute(messageData.Path);
        if (!routingResult.RouteFound) return routingResult;
        
        try
        {
            var metrics = metricsHost.GetMetrics();
            var client = clientFactory.CreateClient(Proxy.ProxyClientWithRetry);
            var request = messageData.CreateForwardingRequestAsOriginal(routingResult.RouteUrl);
            
            metrics.StartRoutedRequest();
            var response = await client.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();
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
        var routingResult = messageRoutes.GetForkedRoute(messageData.Path);
        if (!routingResult.RouteFound) return routingResult;
        
        try
        {
            var metrics = metricsHost.GetMetrics();
            var client = clientFactory.CreateClient(Proxy.ProxyClientWithRetry);
            var request = messageData.CreateForwardingRequestAsJson(routingResult.RouteUrl);
            
            metrics.StartForkedRequest();
            var response = await client.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();
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