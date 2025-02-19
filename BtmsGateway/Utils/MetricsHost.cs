using System.Diagnostics.Metrics;

namespace BtmsGateway.Utils;

public class MetricsHost
{
    public const string MeterName = "Btms.Gateway";

    public readonly Counter<long> RequestRouted;
    public readonly Counter<long> RequestForked;
    public readonly Histogram<long> TotalRequestDuration;
    public readonly Histogram<long> RoutedRequestDuration;
    public readonly Histogram<long> ForkedRequestDuration;

    public MetricsHost(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MeterName);
        RequestRouted = meter.CreateCounter<long>("btms.gateway.routed", "requests", "Number of routed requests made");
        RequestForked = meter.CreateCounter<long>("btms.gateway.forked", "requests", "Number of forked requests made");
        TotalRequestDuration = meter.CreateHistogram<long>("btms.gateway.duration.total", "ms", "Duration of routing from receiving request to returning routed response");
        RoutedRequestDuration = meter.CreateHistogram<long>("btms.gateway.duration.routed", "ms", "Duration of routed request/response");
        ForkedRequestDuration = meter.CreateHistogram<long>("btms.gateway.duration.forked", "ms", "Duration of forked request/response");
    }

    public IMetrics GetMetrics() => new Metrics(this);
}