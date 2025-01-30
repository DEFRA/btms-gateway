using System.Net;
using BtmsGateway.Middleware;
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
        if (!routingResult.RouteFound || routingResult.RouteLinkType == LinkType.None) return routingResult;
        
        try
        {
            var metrics = metricsHost.GetMetrics();
            var client = clientFactory.CreateClient(Proxy.RoutedClientWithRetry);
            var request = routingResult.ConvertRoutedContentToJson 
                ? messageData.CreateForwardingRequestAsJson(routingResult.FullRouteLink, routingResult.RouteHostHeader, routingResult.MessageBodyDepth) 
                : messageData.CreateForwardingRequestAsOriginal(routingResult.FullRouteLink, routingResult.RouteHostHeader);
            
            metrics.StartRoutedRequest();
            var response = await client.SendAsync(request);
            var content = response.StatusCode == HttpStatusCode.NoContent ? null : await response.Content.ReadAsStringAsync();
            metrics.RecordRoutedRequest();
            
            return routingResult with { RoutingSuccessful = response.IsSuccessStatusCode, ResponseContent = content, StatusCode = response.StatusCode, ResponseDate = response.Headers.Date };
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error routing");
            return routingResult with { StatusCode = HttpStatusCode.ServiceUnavailable, ErrorMessage = $"Error routing - {ex.Message} - {ex.InnerException?.Message}" };
        }
    }
    
    public async Task<RoutingResult> Fork(MessageData messageData)
    {
        var routingResult = messageRoutes.GetRoute(messageData.Path);
        if (!routingResult.RouteFound || routingResult.ForkLinkType == LinkType.None) return routingResult;
        
        try
        {
            var metrics = metricsHost.GetMetrics();
            var client = clientFactory.CreateClient(Proxy.ForkedClientWithRetry);
            var request = routingResult.ConvertForkedContentToJson 
                ? messageData.CreateForwardingRequestAsJson(routingResult.FullForkLink, routingResult.ForkHostHeader, routingResult.MessageBodyDepth) 
                : messageData.CreateForwardingRequestAsOriginal(routingResult.FullForkLink, routingResult.ForkHostHeader);
            
            metrics.StartForkedRequest();
            var response = await client.SendAsync(request);
            var content = response.StatusCode == HttpStatusCode.NoContent ? null : await response.Content.ReadAsStringAsync();
            metrics.RecordForkedRequest();
            
            return routingResult with { RoutingSuccessful = response.IsSuccessStatusCode, ResponseContent = content, StatusCode = response.StatusCode, ResponseDate = response.Headers.Date };
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error forking");
            return routingResult with { StatusCode = HttpStatusCode.ServiceUnavailable, ErrorMessage = $"Error forking - {ex.Message} - {ex.InnerException?.Message}" };
        }
    }
}