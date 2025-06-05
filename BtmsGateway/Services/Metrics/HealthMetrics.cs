using System.Diagnostics;
using System.Diagnostics.Metrics;
using Amazon.CloudWatch.EMF.Model;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using ILogger = Serilog.ILogger;

namespace BtmsGateway.Services.Metrics;

public interface IHealthMetrics
{
    void ReportHealth(HealthReport report);
}

public class HealthMetrics : IHealthMetrics
{
    private readonly ILogger _logger;
    private readonly Gauge<int> health;

    public HealthMetrics(IMeterFactory meterFactory, ILogger logger)
    {
        var meter = meterFactory.Create(MetricsConstants.MetricNames.MeterName);

        health = meter.CreateGauge<int>(
            MetricsConstants.InstrumentNames.Health,
            Unit.NONE.ToString(),
            description: "Application and Dependency Health"
        );

        _logger = logger;
    }

    public void ReportHealth(HealthReport report)
    {
        try
        {
            health.Record((int)report.Status, BuildOverallTags());
            _logger.Debug("Health Report for BTMS Gateway with Status {Status} recorded", (int)report.Status);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error attempting to record health for BTMS Gateway");
        }

        foreach (var healthReportEntry in report.Entries)
        {
            try
            {
                health.Record((int)healthReportEntry.Value.Status, BuildComponentTags(healthReportEntry));
                _logger.Debug(
                    "Health Report for {Component} with Status {Status} recorded",
                    healthReportEntry.Key,
                    (int)healthReportEntry.Value.Status
                );
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error attempting to record health for {Component}", healthReportEntry.Key);
            }
        }
    }

    private static TagList BuildComponentTags(KeyValuePair<string, HealthReportEntry> reportEntry)
    {
        var tags = new TagList
        {
            { MetricsConstants.HealthTags.Service, Process.GetCurrentProcess().ProcessName },
            { MetricsConstants.HealthTags.Component, reportEntry.Key },
            { MetricsConstants.HealthTags.Description, reportEntry.Value.Description },
        };

        foreach (
            var keyValuePair in reportEntry.Value.Data.Where(kvp =>
                !string.Equals(kvp.Key, "content", StringComparison.InvariantCultureIgnoreCase)
            )
        )
        {
            tags.Add(keyValuePair.Key, keyValuePair.Value);
        }

        return tags;
    }

    private static TagList BuildOverallTags()
    {
        return new TagList
        {
            { MetricsConstants.HealthTags.Service, Process.GetCurrentProcess().ProcessName },
            { MetricsConstants.HealthTags.Component, "BTMS Gateway" },
            { MetricsConstants.HealthTags.Description, "Overall health of the BTMS Gateway" },
        };
    }
}
