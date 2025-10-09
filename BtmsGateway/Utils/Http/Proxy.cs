using System.Diagnostics.CodeAnalysis;
using System.Net;
using BtmsGateway.Config;
using BtmsGateway.Services.Health;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
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
    public const string ForkedClientWithRetry = "forked-with-retry";
    public const string DecisionComparerProxyClientWithRetry = "decision-comparer-proxy-with-retry";

    [ExcludeFromCodeCoverage]
    public static IHttpClientBuilder AddHttpProxyClientWithoutRetry(this IServiceCollection services)
    {
        return services
            .AddHttpClient(ProxyClientWithoutRetry)
            .ConfigurePrimaryHttpMessageHandler(ConfigurePrimaryHttpMessageHandler)
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
            .AddPolicyHandler(strategy);
    }

    [ExcludeFromCodeCoverage]
    public static IHttpClientBuilder AddHttpProxyForkedClientWithRetry(
        this IServiceCollection services,
        int httpClientTimeoutInSeconds
    )
    {
        var strategy = Policy.WrapAsync(
            WaitAndRetryAsync,
            Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(httpClientTimeoutInSeconds))
        );

        return services
            .AddHttpClient(ForkedClientWithRetry)
            .ConfigurePrimaryHttpMessageHandler(ConfigurePrimaryHttpMessageHandler)
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
            .ConfigurePrimaryHttpMessageHandler(ConfigurePrimaryHttpMessageHandler)
            .AddPolicyHandler(strategy);
    }

    [ExcludeFromCodeCoverage]
    private static HttpClientHandler ConfigurePrimaryHttpMessageHandler()
    {
        var proxyUri = Environment.GetEnvironmentVariable("CDP_HTTPS_PROXY");
        return CreateHttpClientHandler(proxyUri);
    }

    public static HttpClientHandler CreateHttpClientHandler(string? proxyUri)
    {
        var proxy = CreateProxy(proxyUri);
        return new HttpClientHandler { Proxy = proxy, UseProxy = proxyUri != null };
    }

    public static WebProxy CreateProxy(string? proxyUri)
    {
        var proxy = new WebProxy { BypassProxyOnLocal = true };
        if (proxyUri != null)
        {
            ConfigureProxy(proxy, proxyUri);
        }
        return proxy;
    }

    public static void ConfigureProxy(WebProxy proxy, string proxyUri)
    {
        var uri = new UriBuilder(proxyUri);

        var credentials = GetCredentialsFromUri(uri);
        if (credentials != null)
        {
            proxy.Credentials = credentials;
        }

        // Remove credentials from URI to so they don't get logged.
        uri.UserName = "";
        uri.Password = "";
        proxy.Address = uri.Uri;
    }

    private static NetworkCredential? GetCredentialsFromUri(UriBuilder uri)
    {
        var username = uri.UserName;
        var password = uri.Password;
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            return null;
        return new NetworkCredential(username, password);
    }
}
