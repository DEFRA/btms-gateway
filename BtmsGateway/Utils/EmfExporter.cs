using System.Diagnostics;
using System.Diagnostics.Metrics;
using Amazon.CloudWatch.EMF.Logger;
using Amazon.CloudWatch.EMF.Model;
using Humanizer;
using ILogger = Serilog.ILogger;

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
            EmfExporter.Init(builder.ApplicationServices.GetRequiredService<ILogger>(), ns!);
        }

        return builder;
    }
}

public static class EmfExporter
{
    private static readonly MeterListener MeterListener = new();
    private static ILogger _logger = null!;
    private static string? _awsNamespace;

    private const string DefaultUnitCount = "Count";
    private static readonly Dictionary<string, Unit> UnitsMapper = new()
    {
        { DefaultUnitCount, Unit.COUNT },
        { MetricsHost.UnitsRequests, Unit.COUNT },
        { MetricsHost.UnitsMs, Unit.MILLISECONDS },
    };

    public static void Init(ILogger logger, string? awsNamespace)
    {
        _logger = logger;
        _awsNamespace = awsNamespace;
        MeterListener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Name is MetricsHost.MeterName)
            {
                listener.EnableMeasurementEvents(instrument);
                _logger.Information("METRICS - Enable monitoring events");
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
                if (string.IsNullOrWhiteSpace(tag.Value?.ToString())) continue;
                dimensionSet.AddDimension(tag.Key.Dehumanize().Pascalize(), tag.Value?.ToString());
            }

            // If the request contains a w3c trace id, let's embed it in the logs
            // Otherwise we'll include the TraceIdentifier which is the connectionId:requestCount
            // identifier.
            // https://www.w3.org/TR/trace-context/#traceparent-header
            if (!string.IsNullOrEmpty(Activity.Current?.Id))
            {
                metricsLogger.PutProperty("TraceId", Activity.Current.Id);
            }

            if (!string.IsNullOrEmpty(Activity.Current?.TraceStateString))
            {
                metricsLogger.PutProperty("TraceState", Activity.Current.TraceStateString);
            }

            metricsLogger.SetDimensions(dimensionSet);
            var name = instrument.Name.Dehumanize().Pascalize();

            _logger.Information("METRICS - Set metadata for instrument {Name}", name);

            metricsLogger.PutMetric(name, Convert.ToDouble(measurement), UnitsMapper[instrument.Unit ?? DefaultUnitCount]);
            metricsLogger.Flush();

            _logger.Information("METRICS - Set and flush measurement {Measurement} {Unit} for instrument {Name}", measurement, instrument.Unit, name);
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Failed to push EMF metric");
        }
    }
}