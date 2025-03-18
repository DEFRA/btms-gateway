using System.Diagnostics;
using BtmsGateway.Middleware;
using BtmsGateway.Services.Routing;

namespace BtmsGateway.Utils;

public interface IMetrics
{
    public void StartRoutedRequest();
    public void RecordRoutedRequest(MessageData messageData, RoutingResult routingResult);

    public void StartForkedRequest();
    public void RecordForkedRequest(MessageData messageData, RoutingResult routingResult);
}

public class Metrics(MetricsHost metricsHost) : IMetrics
{
    private static ReadOnlySpan<KeyValuePair<string, object?>> CompletedList(MessageData messageData, RoutingResult? routingResult = null)
    {
        return new KeyValuePair<string, object?>[]
        {
            new("correlation-id", messageData.CorrelationId),
            new("originating-url", messageData.Url),
            new("method", messageData.Method),
            new("content-type", messageData.OriginalContentType),
            new("path", messageData.Path),
            new("country-code", messageData.ContentMap.CountryCode),
            new("route-name", routingResult?.RouteName),
            new("route-found", routingResult?.RouteFound),
            new("routing-successful", routingResult?.RoutingSuccessful),
            new("forward-url", routingResult?.FullRouteLink),
            new("status-code", routingResult?.StatusCode)
        };
    }

    public void StartRoutedRequest() => _routedRequestDuration.Start();
    public void RecordRoutedRequest(MessageData messageData, RoutingResult routingResult) => metricsHost.RoutedRequestDuration.Record(_routedRequestDuration.ElapsedMilliseconds, CompletedList(messageData, routingResult));

    public void StartForkedRequest() => _forkedRequestDuration.Start();
    public void RecordForkedRequest(MessageData messageData, RoutingResult routingResult) => metricsHost.ForkedRequestDuration.Record(_forkedRequestDuration.ElapsedMilliseconds, CompletedList(messageData, routingResult));

    private readonly Stopwatch _routedRequestDuration = new();
    private readonly Stopwatch _forkedRequestDuration = new();
}