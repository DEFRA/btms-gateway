using System.Diagnostics.Metrics;

namespace BtmsGateway.Services.Metrics;

public class MetricsHost
{
    public const string MeterName = "Btms.Gateway";

    public const string UnitsMs = "ms";
    public const string UnitsRequests = "requests";

    public readonly Histogram<long> RoutedRequestDuration;
    public readonly Histogram<long> ForkedRequestDuration;
    public readonly Counter<long> RoutingError;

    public MetricsHost(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MeterName);
        RoutedRequestDuration = meter.CreateHistogram<long>(
            "RoutedRequestDuration",
            UnitsMs,
            "Duration of routed request to Existing"
        );
        ForkedRequestDuration = meter.CreateHistogram<long>(
            "BtmsQueuedDuration",
            UnitsMs,
            "Duration of queued request to BTMS"
        );
        RoutingError = meter.CreateCounter<long>("RoutingError", UnitsRequests, "Count of routing errors");
    }

    public IMetrics GetMetrics() => new Metric(this);
}
