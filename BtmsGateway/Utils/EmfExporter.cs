using System.Diagnostics;
using System.Diagnostics.Metrics;
using Amazon.CloudWatch.EMF.Logger;
using Amazon.CloudWatch.EMF.Model;
using Humanizer;

namespace BtmsGateway.Utils;

public static class EmfExportExtensions
{
    public static IApplicationBuilder UseEmfExporter(this IApplicationBuilder builder)
    {
        var config = builder.ApplicationServices.GetRequiredService<IConfiguration>();
        var enabled = config.GetValue("AWS_EMF_ENABLED", true);

        if (enabled)
        {
            var ns = config.GetValue<string>("AWS_EMF_NAMESPACE");
            EmfExporter.Init(builder.ApplicationServices.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(EmfExporter)), ns!);
        }

        return builder;
    }
}

public static class EmfExporter
{
    private static readonly MeterListener MeterListener = new();
    private static ILogger _log = null!;
    private static string? _awsNamespace;
    public static void Init(ILogger logger, string? awsNamespace)
    {
        _log = logger;
        _awsNamespace = awsNamespace;
        MeterListener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Name is MetricsHost.MeterName)
            {
                listener.EnableMeasurementEvents(instrument);
            }
        };

        MeterListener.SetMeasurementEventCallback<int>(OnMeasurementRecorded);
        MeterListener.SetMeasurementEventCallback<long>(OnMeasurementRecorded);
        MeterListener.SetMeasurementEventCallback<double>(OnMeasurementRecorded);
        MeterListener.Start();
    }

    private static void OnMeasurementRecorded<T>(
        Instrument instrument,
        T measurement,
        ReadOnlySpan<KeyValuePair<string, object?>> tags,
        object? state)
    {
        try
        {
            using var metricsLogger = new MetricsLogger();

            metricsLogger.SetNamespace(_awsNamespace);
            var dimensionSet = new DimensionSet();
            foreach (var tag in tags)
            {
                dimensionSet.AddDimension(tag.Key, tag.Value?.ToString());
            }

            // If the request contains a w3c trace id, let's embed it in the logs
            // Otherwise we'll include the TraceIdentifier which is the connectionId:requestCount
            // identifier.
            // https://www.w3.org/TR/trace-context/#traceparent-header
            if (!string.IsNullOrEmpty(Activity.Current?.Id))
            {
                metricsLogger.PutProperty("TraceId", Activity.Current.TraceStateString);
            }

            if (!string.IsNullOrEmpty(Activity.Current?.TraceStateString))
            {
                metricsLogger.PutProperty("TraceState", Activity.Current.TraceStateString);
            }
            metricsLogger.SetDimensions(dimensionSet);
            var name = instrument.Name.Dehumanize().Camelize();
            metricsLogger.PutMetric(name, Convert.ToDouble(measurement), instrument.Unit == "ea" ? Unit.COUNT : Unit.MILLISECONDS);
            metricsLogger.Flush();
        }
        catch (Exception e)
        {
            _log.LogError(e, "Failed to push EMF metric");
        }
    }
}