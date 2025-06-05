using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using BtmsGateway.Config;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using ILogger = Serilog.ILogger;

namespace BtmsGateway.Services.Health;

[ExcludeFromCodeCoverage]
public class ApiHealthCheck<T>(string name, string checkEndpoint, T options, ILogger logger) : IHealthCheck
    where T : HttpClientConfigurableOptions
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = new()
    )
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(ConfigureHealthChecks.Timeout);

        Exception? exception = null;
        HttpResponseMessage? response = null;
        try
        {
            using var httpClient = new HttpClient();

            options.Configure(httpClient);

            response = await httpClient.GetAsync(checkEndpoint, cancellationToken);
        }
        catch (TaskCanceledException ex)
        {
            logger.Warning(ex, "HEALTH - Retrieving API URL timed out for API {API}", name);
            exception = new TimeoutException(
                $"The API check was cancelled, probably because it timed out after {ConfigureHealthChecks.Timeout.TotalSeconds} seconds"
            );
        }
        catch (Exception ex)
        {
            logger.Warning(ex, "HEALTH - Retrieving API URL failed for API {API}", name);
            exception = ex;
        }

        var healthStatus = HealthStatus.Healthy;
        var data = new Dictionary<string, object> { { "api", name } };
        if (response != null)
        {
            if (!response.IsSuccessStatusCode)
                healthStatus = HealthStatus.Unhealthy;

            data.Add("http-status-code", response.StatusCode);
        }

        if (exception != null)
        {
            healthStatus = HealthStatus.Unhealthy;
            data.Add("error", $"{exception.Message} - {exception.InnerException?.Message}");
        }

        return new HealthCheckResult(
            status: healthStatus,
            description: $"API: {string.Join(' ', Regex.Matches(name, "[A-Z][a-z]+", RegexOptions.None, TimeSpan.FromMilliseconds(200)))}",
            exception: exception,
            data: data
        );
    }
}
