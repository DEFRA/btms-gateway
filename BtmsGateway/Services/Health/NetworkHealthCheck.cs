using BtmsGateway.Services.Checking;
using BtmsGateway.Utils.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace BtmsGateway.Services.Health;

public class NetworkHealthCheck(
    string name,
    HealthCheckUrl healthCheckUrl,
    IHttpClientFactory httpClientFactory,
    ILogger<NetworkHealthCheck> logger
) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = new()
    )
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(ConfigureHealthChecks.Timeout);

        var client = httpClientFactory.CreateClient(Proxy.RoutedClientWithRetry);
        var request = new HttpRequestMessage(HttpMethod.Parse(healthCheckUrl.Method), healthCheckUrl.Url);
        if (healthCheckUrl.HostHeader != null)
            request.Headers.TryAddWithoutValidation("host", healthCheckUrl.HostHeader);

        HttpResponseMessage? response = null;
        string? content = null;
        Exception? exception = null;
        try
        {
            response = await client.SendAsync(request, cts.Token);
            content = await response.Content.ReadAsStringAsync(cts.Token);
        }
        catch (TaskCanceledException ex)
        {
            logger.LogWarning(
                ex,
                "HEALTH - Checking network connection timed out for Uri {Uri}",
                healthCheckUrl.Url
            );
            exception = new TimeoutException(
                $"The network check has cancelled, probably because it timed out after {ConfigureHealthChecks.Timeout.TotalSeconds} seconds",
                ex
            );
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "HEALTH - Checking network connection failed for Uri {Uri}",
                healthCheckUrl.Url
            );
            exception = ex;
        }

        var statusCode = (int?)response?.StatusCode;
        var data = new Dictionary<string, object>
        {
            { "route", healthCheckUrl.Url },
            { "host", healthCheckUrl.HostHeader ?? "" },
            { "method", healthCheckUrl.Method },
            { "status", $"{statusCode} {response?.StatusCode}".Trim() },
            { "content", content ?? "" },
        };

        var healthStatus =
            response?.IsSuccessStatusCode == true
            || (
                statusCode is not null
                && healthCheckUrl.AdditionalSuccessStatuses is not null
                && healthCheckUrl.AdditionalSuccessStatuses.Contains(statusCode.Value)
            )
                ? HealthStatus.Healthy
                : HealthStatus.Degraded;
        if (exception != null)
        {
            healthStatus = HealthStatus.Degraded;
            data.Add("error", $"{exception.Message} - {exception.InnerException?.Message}");
        }

        return new HealthCheckResult(
            status: healthStatus,
            description: $"Network route: {name.Replace('_', ' ')}",
            exception: exception,
            data: data
        );
    }
}
