using BtmsGateway.Services.Checking;
using BtmsGateway.Utils.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using ILogger = Serilog.ILogger;

namespace BtmsGateway.Services.Health;

public class NetworkHealthCheck(string name, HealthCheckUrl healthCheckUrl, IHttpClientFactory httpClientFactory, ILogger logger) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = new())
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(ConfigureHealthChecks.Timeout);

        var client = httpClientFactory.CreateClient(Proxy.RoutedClientWithRetry);
        var request = new HttpRequestMessage(HttpMethod.Parse(healthCheckUrl.Method), healthCheckUrl.Url);
        if (healthCheckUrl.HostHeader != null) request.Headers.TryAddWithoutValidation("host", healthCheckUrl.HostHeader);
        if (healthCheckUrl.PostData != null) request.Content = new StreamContent(new FileStream(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Services", "Fixtures", healthCheckUrl.PostData), FileMode.Open, FileAccess.Read, FileShare.Read));

        HttpResponseMessage? response = null;
        string? content = null;
        Exception? exception = null;
        try
        {
            response = await client.SendAsync(request, cts.Token);
            content = await response.Content.ReadAsStringAsync(cts.Token);
        }
        catch (TaskCanceledException)
        {
            logger.Warning("HEALTH - Checking network connection timed out for queue {QueueUrl}", healthCheckUrl.Url);
            exception = new TimeoutException($"The network check cas cancelled, probably because it timed out after {ConfigureHealthChecks.Timeout.TotalSeconds} seconds");
        }
        catch (Exception ex)
        {
            logger.Warning(ex, "HEALTH - Checking network connection failed for queue {QueueUrl}", healthCheckUrl.Url);
            exception = ex;
        }

        var data = new Dictionary<string, object>
        {
            { "route", healthCheckUrl.Url },
            { "host", healthCheckUrl.HostHeader ?? "" },
            { "method", healthCheckUrl.Method },
            { "status", $"{(int?)response?.StatusCode} {response?.StatusCode}".Trim() },
            { "content", content ?? "" }
        };

        var healthStatus = response?.IsSuccessStatusCode == true ? HealthStatus.Healthy : HealthStatus.Degraded;
        if (exception != null)
        {
            healthStatus = HealthStatus.Unhealthy;
            data.Add("error", $"{exception.Message} - {exception.InnerException?.Message}");
        }

        return new HealthCheckResult(
            status: healthStatus,
            description: $"Network route: {name.Replace('_', ' ')}",
            exception: exception,
            data: data);
    }
}