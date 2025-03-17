using System.Diagnostics.Metrics;

namespace BtmsGateway.Utils;

public class MetricsHost
{
    public const string MeterName = "Btms.Gateway";

    public const string UnitsMs = "ms";
    public const string UnitsRequests = "requests";

    public readonly Counter<long> RequestRouted;
    public readonly Counter<long> RequestForked;
    public readonly Histogram<long> TotalRequestDuration;
    public readonly Histogram<long> RoutedRequestDuration;
    public readonly Histogram<long> ForkedRequestDuration;

    public MetricsHost(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MeterName);
        RequestRouted = meter.CreateCounter<long>("RoutedRequest", UnitsRequests, "Number of routed requests made");
        RequestForked = meter.CreateCounter<long>("ForkedRequest", UnitsRequests, "Number of forked requests made");
        TotalRequestDuration = meter.CreateHistogram<long>("TotalRequestDuration", UnitsMs, "Duration of routing from receiving request to returning routed response");
        RoutedRequestDuration = meter.CreateHistogram<long>("RoutedRequestDuration", UnitsMs, "Duration of routed request/response");
        ForkedRequestDuration = meter.CreateHistogram<long>("ForkedRequestDuration", UnitsMs, "Duration of forked request/response");
    }

    public IMetrics GetMetrics() => new Metrics(this);
}