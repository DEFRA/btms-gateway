using System.Diagnostics;
using BtmsGateway.Middleware;
using BtmsGateway.Services.Routing;

namespace BtmsGateway.Utils;

public interface IMetrics
{
    public void RequestRouted(MessageData messageData, RoutingResult routingResult);

    public void RequestForked(MessageData messageData, RoutingResult routingResult);
    public void StartTotalRequest();
    public void RecordTotalRequest();

    public void StartRoutedRequest();
    public void RecordRoutedRequest();

    public void StartForkedRequest();
    public void RecordForkedRequest();
}

public class Metrics(MetricsHost metricsHost) : IMetrics
{
    private static TagList CompletedList(MessageData messageData, RoutingResult routingResult)
    {
        return new TagList
        {
            { "correlation-id", messageData.CorrelationId },
            { "originating-url", messageData.Url },
            { "method", messageData.Method },
            { "content-type", messageData.OriginalContentType },
            { "path", messageData.Path },
            { "ched-type", messageData.ContentMap.ChedType },
            { "country-code", messageData.ContentMap.CountryCode },
            { "route-name", routingResult.RouteName },
            { "route-found", routingResult.RouteFound },
            { "routing-successful", routingResult.RoutingSuccessful },
            { "forward-url", routingResult.FullRouteLink },
            { "status-code", routingResult.StatusCode }
        };
    }
    
    public void RequestRouted(MessageData messageData, RoutingResult routingResult) => metricsHost.RequestRouted.Add(1, CompletedList(messageData, routingResult));
    
    public void RequestForked(MessageData messageData, RoutingResult routingResult) => metricsHost.RequestForked.Add(1, CompletedList(messageData, routingResult));

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
