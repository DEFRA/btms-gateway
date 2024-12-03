using System.Diagnostics;
using BtmsGateway.Services;
using BtmsGateway.Services.Routing;

namespace BtmsGateway.Utils;

public class Metrics(MetricsHost metricsHost)
{
    public void RequestRouted(MessageData messageData, RoutingResult routingResult) => metricsHost.RequestRouted.Add(1, CompletedList(messageData, routingResult));
    
    public void RequestForked(MessageData messageData, RoutingResult routingResult) => metricsHost.RequestForked.Add(1, CompletedList(messageData, routingResult));

    private static TagList CompletedList(MessageData messageData, RoutingResult routingResult)
    {
        return new TagList
        {
            { "correlation-id", messageData.CorrelationId },
            { "originating-url", messageData.Url },
            { "method", messageData.Method },
            { "content-type", messageData.ContentType },
            { "path", messageData.Path },
            { "ched-type", messageData.ContentMap.ChedType },
            { "country-code", messageData.ContentMap.CountryCode },
            { "route-name", routingResult.RouteName },
            { "route-found", routingResult.RouteFound },
            { "routing-successful", routingResult.RoutingSuccessful },
            { "forward-url", routingResult.FullRouteUrl },
            { "status-code", routingResult.StatusCode }
        };
    }
    
    public void StartTotalRequest() => _totalRequestDuration.Start();
    public void RecordTotalRequest() => metricsHost.TotalRequestDuration.Record(_totalRequestDuration.ElapsedMilliseconds);
    
    public void StartRoutedRequest() => _routedRequestDuration.Start();
    public void RecordRoutedRequest() => metricsHost.RoutedRequestDuration.Record(_routedRequestDuration.ElapsedMilliseconds);
    
    public void StartForkedRequest() => _forkedRequestDuration.Start();
    public void RecordForkedRequest() => metricsHost.ForkedRequestDuration.Record(_forkedRequestDuration.ElapsedMilliseconds);

    private readonly Stopwatch _totalRequestDuration = new();
    private readonly Stopwatch _routedRequestDuration = new();
    private readonly Stopwatch _forkedRequestDuration = new();
}
