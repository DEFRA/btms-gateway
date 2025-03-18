using System.Diagnostics.Metrics;

namespace BtmsGateway.Utils;

public class MetricsHost
{
    public const string MeterName = "Btms.Gateway";

    public const string UnitsMs = "ms";
    public const string UnitsRequests = "requests";

    public readonly Histogram<long> RoutedRequestDuration;
    public readonly Histogram<long> ForkedRequestDuration;

    public MetricsHost(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MeterName);
        RoutedRequestDuration = meter.CreateHistogram<long>("RoutedRequestDuration", UnitsMs, "Duration of routed request/response");
        ForkedRequestDuration = meter.CreateHistogram<long>("ForkedRequestDuration", UnitsMs, "Duration of forked request/response");
    }

    public IMetrics GetMetrics() => new Metrics(this);
}