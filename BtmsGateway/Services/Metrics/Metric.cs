using System.Diagnostics;
using BtmsGateway.Services.Routing;

namespace BtmsGateway.Services.Metrics;

public interface IMetrics
{
    public void StartRoutedRequest();
    public void RecordRoutedRequest(RoutingResult routingResult);

    public void StartForkedRequest();
    public void RecordForkedRequest(RoutingResult routingResult);
}

public class Metric(MetricsHost metricsHost) : IMetrics
{
    private static ReadOnlySpan<KeyValuePair<string, object?>> RequestDurationArgs(RoutingResult routingResult)
    {
        return new KeyValuePair<string, object?>[]
        {
            new("routing-successful", routingResult.RoutingSuccessful),
            new("legend", routingResult.Legend)
        };
    }

    public void StartRoutedRequest() => _routedRequestDuration.Start();

    public void RecordRoutedRequest(RoutingResult routingResult)
    {
        metricsHost.RoutedRequestDuration.Record(_routedRequestDuration.ElapsedMilliseconds, RequestDurationArgs(routingResult));
        if (!routingResult.RoutingSuccessful)
            metricsHost.RoutingError.Add(1, new KeyValuePair<string, object?>[] { new("route-url", routingResult.FullRouteLink) });
    }

    public void StartForkedRequest() => _forkedRequestDuration.Start();

    public void RecordForkedRequest(RoutingResult routingResult)
    {
        metricsHost.ForkedRequestDuration.Record(_forkedRequestDuration.ElapsedMilliseconds, RequestDurationArgs(routingResult));
        if (!routingResult.RoutingSuccessful)
            metricsHost.RoutingError.Add(1, new KeyValuePair<string, object?>[] { new("route-url", routingResult.FullForkLink) });
    }

    private readonly Stopwatch _routedRequestDuration = new();
    private readonly Stopwatch _forkedRequestDuration = new();
}