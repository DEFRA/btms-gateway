using System.Diagnostics;
using System.Diagnostics.Metrics;
using Amazon.CloudWatch.EMF.Model;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace BtmsGateway.Services.Metrics;

public interface IHealthMetrics
{
    void ReportHealth(HealthReport report);
}

public class HealthMetrics : IHealthMetrics
{
    private readonly Gauge<int> health;

    public HealthMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MetricsConstants.MetricNames.MeterName);

        health = meter.CreateGauge<int>(
            MetricsConstants.InstrumentNames.Health,
            Unit.NONE.ToString(),
            description: "Application and Dependency Health"
        );
    }

    public void ReportHealth(HealthReport report)
    {
        health.Record((int)report.Status, BuildOverallTags());

        foreach (var healthReportEntry in report.Entries)
        {
            health.Record((int)healthReportEntry.Value.Status, BuildComponentTags(healthReportEntry));
        }
    }

    private static TagList BuildComponentTags(KeyValuePair<string, HealthReportEntry> reportEntry)
    {
        var tags = new TagList
        {
            { MetricsConstants.HealthTags.Service, Process.GetCurrentProcess().ProcessName },
            { MetricsConstants.HealthTags.Component, reportEntry.Key },
            { MetricsConstants.HealthTags.Description, reportEntry.Value.Description },
            { MetricsConstants.HealthTags.InstanceId, InstanceMetadata.InstanceId },
        };

        foreach (var keyValuePair in reportEntry.Value.Data)
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
            { MetricsConstants.HealthTags.InstanceId, InstanceMetadata.InstanceId },
        };
    }
}
