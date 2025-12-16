using System.Diagnostics.CodeAnalysis;
using System.Net;
using BtmsGateway.Services.Health;
using Polly;
using Polly.Extensions.Http;
using Polly.Retry;
using Polly.Timeout;
using Environment = System.Environment;

namespace BtmsGateway.Utils.Http;

[ExcludeFromCodeCoverage]
public static class Proxy
{
    public static readonly int DefaultHttpClientTimeoutSeconds = 10;
    public static readonly int DefaultCdsHttpClientRetries = 3;

    public const string ProxyClientWithoutRetry = "proxy";
    public const string CdsProxyClientWithRetry = "proxy-with-retry";
    public const string RoutedClientWithRetry = "routed-with-retry";

    [ExcludeFromCodeCoverage]
    public static IHttpClientBuilder AddHttpProxyClientWithoutRetry(this IServiceCollection services)
    {
        return services
            .AddHttpClient(ProxyClientWithoutRetry)
            .ConfigurePrimaryHttpMessageHandler(ConfigurePrimaryHttpMessageHandler)
            .AddHttpMessageHandler<ProxyLoggingHandler>()
            .ConfigureHttpClient(client => client.Timeout = ConfigureHealthChecks.Timeout);
    }

    private static readonly AsyncRetryPolicy<HttpResponseMessage> WaitAndRetryAsync = HttpPolicyExtensions
        .HandleTransientHttpError()
        .Or<TimeoutRejectedException>()
        .WaitAndRetryAsync(3, _ => TimeSpan.FromMilliseconds(1000));

    [ExcludeFromCodeCoverage]
    public static IHttpClientBuilder AddHttpProxyRoutedClientWithRetry(
        this IServiceCollection services,
        int httpClientTimeoutInSeconds
    )
    {
        var strategy = Policy.WrapAsync(
            WaitAndRetryAsync,
            Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(httpClientTimeoutInSeconds))
        );

        return services
            .AddHttpClient(RoutedClientWithRetry)
            .ConfigurePrimaryHttpMessageHandler(ConfigurePrimaryHttpMessageHandler)
            .AddHttpMessageHandler<ProxyLoggingHandler>()
            .AddPolicyHandler(strategy);
    }

    [ExcludeFromCodeCoverage]
    public static IHttpClientBuilder AddHttpProxyClientWithRetry(
        this IServiceCollection services,
        int httpClientTimeoutInSeconds,
        int httpClientRetryCount
    )
    {
        // Uses its own retry policy as this client affects SQS consumption, and the total retry time potentially exceeds the SQS Visibility Timeout if the number of retries is higher
        // Left it as its own separate retry configuration to make any future updates easier
        var strategy = Policy.WrapAsync(
            HttpPolicyExtensions
                .HandleTransientHttpError()
                .Or<TimeoutRejectedException>()
                .WaitAndRetryAsync(httpClientRetryCount, _ => TimeSpan.FromMilliseconds(1000)),
            Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(httpClientTimeoutInSeconds))
        );

        return services
            .AddHttpClient(CdsProxyClientWithRetry)
            .AddHttpMessageHandler<ProxyLoggingHandler>()
            .ConfigurePrimaryHttpMessageHandler(ConfigurePrimaryHttpMessageHandler)
            .AddPolicyHandler(strategy);
    }

    [ExcludeFromCodeCoverage]
    private static HttpClientHandler ConfigurePrimaryHttpMessageHandler()
    {
        var proxyUri = Environment.GetEnvironmentVariable("HTTPS_PROXY");
        return CreateHttpClientHandler(proxyUri);
    }

    public static HttpClientHandler CreateHttpClientHandler(string? proxyUri)
    {
        var proxy = CreateProxy(proxyUri);
        return new HttpClientHandler { Proxy = proxy, UseProxy = proxyUri != null };
    }

    public static WebProxy CreateProxy(string? proxyUri)
    {
        var proxy = new WebProxy { BypassProxyOnLocal = false };
        if (proxyUri != null)
        {
            proxy.Address = new UriBuilder(proxyUri).Uri;
        }
        return proxy;
    }
}
