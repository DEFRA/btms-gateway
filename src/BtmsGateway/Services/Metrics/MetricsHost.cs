using System.Diagnostics.Metrics;
using Amazon.CloudWatch.EMF.Model;

namespace BtmsGateway.Services.Metrics;

public class MetricsHost
{
    public const string MeterName = "Btms.Gateway";

    public readonly Histogram<long> RoutedRequestDuration;
    public readonly Histogram<long> ForkedRequestDuration;
    public readonly Counter<long> RoutingError;

    public MetricsHost(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MeterName);
        RoutedRequestDuration = meter.CreateHistogram<long>(
            "RoutedRequestDuration",
            Unit.MILLISECONDS.ToString(),
            "Duration of routed request to Existing"
        );
        ForkedRequestDuration = meter.CreateHistogram<long>(
            "BtmsQueuedDuration",
            Unit.MILLISECONDS.ToString(),
            "Duration of queued request to BTMS"
        );
        RoutingError = meter.CreateCounter<long>("RoutingError", Unit.COUNT.ToString(), "Count of routing errors");
    }

    public IMetrics GetMetrics() => new Metric(this);
}
