using System.Diagnostics;
using BtmsGateway.Middleware;
using BtmsGateway.Services.Routing;

namespace BtmsGateway.Services.Metrics;

public interface IMetrics
{
    public void StartRoutedRequest();
    public void RecordRoutedRequest(MessageData messageData, RoutingResult routingResult);

    public void StartForkedRequest();
    public void RecordForkedRequest(MessageData messageData, RoutingResult routingResult);
}

public class Metric(MetricsHost metricsHost) : IMetrics
{
    private static ReadOnlySpan<KeyValuePair<string, object?>> CompletedList(MessageData messageData, RoutingResult routingResult)
    {
        return new KeyValuePair<string, object?>[]
        {
            new("routing-successful", routingResult.RoutingSuccessful),
            new("path", messageData.Path)
        };
    }

    public void StartRoutedRequest() => _routedRequestDuration.Start();
    public void RecordRoutedRequest(MessageData messageData, RoutingResult routingResult) => metricsHost.RoutedRequestDuration.Record(_routedRequestDuration.ElapsedMilliseconds, CompletedList(messageData, routingResult));

    public void StartForkedRequest() => _forkedRequestDuration.Start();
    public void RecordForkedRequest(MessageData messageData, RoutingResult routingResult) => metricsHost.ForkedRequestDuration.Record(_forkedRequestDuration.ElapsedMilliseconds, CompletedList(messageData, routingResult));

    private readonly Stopwatch _routedRequestDuration = new();
    private readonly Stopwatch _forkedRequestDuration = new();
}