using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace BtmsGateway.Services.Health;

public static class HealthCheckWriter
{
    public static string WriteHealthStatusAsJson(HealthReport healthReport, bool excludeHealthy, bool indented)
    {
        var options = new JsonWriterOptions { Indented = indented };

        using var memoryStream = new MemoryStream();
        using (var jsonWriter = new Utf8JsonWriter(memoryStream, options))
        {
            jsonWriter.WriteStartObject();
            jsonWriter.WriteString("status", healthReport.Status.ToString());
            
            var healthReportEntries = healthReport.Entries.Where(x => !excludeHealthy || x.Value.Status != HealthStatus.Healthy).ToArray();
            if (healthReportEntries.Any())
            {
                jsonWriter.WriteStartObject("results");

                foreach (var healthReportEntry in healthReportEntries)
                {
                    jsonWriter.WriteStartObject(healthReportEntry.Key);
                    jsonWriter.WriteString("status", healthReportEntry.Value.Status.ToString());
                    jsonWriter.WriteString("description", healthReportEntry.Value.Description);
                    jsonWriter.WriteString("exception", $"{healthReportEntry.Value.Exception?.GetType().Name}  {healthReportEntry.Value.Exception?.InnerException?.GetType().Name}".Trim());
                    jsonWriter.WriteStartObject("data");

                    foreach (var item in healthReportEntry.Value.Data)
                    {
                        jsonWriter.WritePropertyName(item.Key);
                        JsonSerializer.Serialize(jsonWriter, item.Value, item.Value.GetType());
                    }

                    jsonWriter.WriteEndObject();
                    jsonWriter.WriteEndObject();
                }

                jsonWriter.WriteEndObject();
            }

            jsonWriter.WriteEndObject();
        }

        return Encoding.UTF8.GetString(memoryStream.ToArray());
    }
}