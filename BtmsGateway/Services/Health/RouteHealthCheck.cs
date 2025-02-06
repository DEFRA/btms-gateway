using BtmsGateway.Services.Checking;
using BtmsGateway.Utils.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace BtmsGateway.Services.Health;

public class RouteHealthCheck(string name, HealthCheckUrl healthCheckUrl, IHttpClientFactory httpClientFactory) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = new())
    {
        var client = httpClientFactory.CreateClient(Proxy.ProxyClientWithoutRetry);
        var request = new HttpRequestMessage(HttpMethod.Parse(healthCheckUrl.Method), healthCheckUrl.Url);
        if (healthCheckUrl.HostHeader != null) request.Headers.TryAddWithoutValidation("host", healthCheckUrl.HostHeader);

        HttpResponseMessage? response = null;
        string? content = null;
        Exception? exception = null;
        try
        {
            response = await client.SendAsync(request, cancellationToken);
            content = await response.Content.ReadAsStringAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            exception = ex;
        }
        
        return new HealthCheckResult(
            status: exception == null ? response?.IsSuccessStatusCode == true ? HealthStatus.Healthy : HealthStatus.Degraded : HealthStatus.Unhealthy, 
            description: $"Route to {name.Replace('_', ' ')}",
            exception: exception,
            data: new Dictionary<string, object>
            {
                { "route", healthCheckUrl.Url},
                { "host", healthCheckUrl.HostHeader ?? ""},
                { "method", healthCheckUrl.Method},
                { "status", $"{(int?)response?.StatusCode} {response?.StatusCode}".Trim() },
                { "content", content ?? "" },
                { "error", exception != null ? $"{exception.Message} - {exception.InnerException?.Message}" : "" }
            });
    }
}