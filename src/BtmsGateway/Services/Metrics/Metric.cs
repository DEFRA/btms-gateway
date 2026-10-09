using System.Diagnostics;
using BtmsGateway.Services.Routing;

namespace BtmsGateway.Services.Metrics;

public interface IMetrics
{
    public void StartRoutedRequest();
    public void RecordRoutedRequest(RoutingResult routingResult);

    public void RecordRoutingError(string routeLink);
}

public class Metric(MetricsHost metricsHost) : IMetrics
{
    private static ReadOnlySpan<KeyValuePair<string, object?>> RequestDurationArgs(RoutingResult routingResult)
    {
        return new KeyValuePair<string, object?>[]
        {
            new("routing-successful", routingResult.RoutingSuccessful),
            new("legend", routingResult.Legend),
        };
    }

    private static ReadOnlySpan<KeyValuePair<string, object?>> RoutingErrorArgs(string? routLink)
    {
        return new KeyValuePair<string, object?>[] { new("route-link", routLink) };
    }

    public void StartRoutedRequest() => _routedRequestDuration.Start();

    public void RecordRoutedRequest(RoutingResult routingResult)
    {
        metricsHost.RoutedRequestDuration.Record(
            _routedRequestDuration.ElapsedMilliseconds,
            RequestDurationArgs(routingResult)
        );
        if (!routingResult.RoutingSuccessful)
            RecordRoutingError(routingResult.FullRouteLink ?? "Unknown");
    }

    public void RecordRoutingError(string routeLink)
    {
        metricsHost.RoutingError.Add(1, RoutingErrorArgs(routeLink));
    }

    private readonly Stopwatch _routedRequestDuration = new();
}
