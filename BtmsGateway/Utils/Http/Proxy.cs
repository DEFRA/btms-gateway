using System.Diagnostics.CodeAnalysis;
using System.Net;
using BtmsGateway.Config;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Extensions.Http;
using Polly.Retry;
using Environment = System.Environment;

namespace BtmsGateway.Utils.Http;

public static class Proxy
{
    public const string ProxyClientWithoutRetry = "proxy";
    public const string ProxyClientWithRetry = "proxy-with-retry";
    public const string RoutedClientWithRetry = "routed-with-retry";
    public const string ForkedClientWithRetry = "forked-with-retry";
    public const string DecisionComparerProxyClientWithRetry = "decision-comparer-proxy-with-retry";

    [ExcludeFromCodeCoverage]
    public static IHttpClientBuilder AddHttpProxyClientWithoutRetry(this IServiceCollection services)
    {
        return services
            .AddHttpClient(ProxyClientWithoutRetry)
            .ConfigurePrimaryHttpMessageHandler(ConfigurePrimaryHttpMessageHandler);
    }

    private static readonly AsyncRetryPolicy<HttpResponseMessage> WaitAndRetryAsync = HttpPolicyExtensions
        .HandleTransientHttpError()
        .WaitAndRetryAsync(3, _ => TimeSpan.FromMilliseconds(1000));

    [ExcludeFromCodeCoverage]
    public static IHttpClientBuilder AddHttpProxyRoutedClientWithRetry(this IServiceCollection services)
    {
        return services
            .AddHttpClient(RoutedClientWithRetry)
            .ConfigurePrimaryHttpMessageHandler(ConfigurePrimaryHttpMessageHandler)
            .AddPolicyHandler(_ => WaitAndRetryAsync);
    }

    [ExcludeFromCodeCoverage]
    public static IHttpClientBuilder AddHttpProxyForkedClientWithRetry(this IServiceCollection services)
    {
        return services
            .AddHttpClient(ForkedClientWithRetry)
            .ConfigurePrimaryHttpMessageHandler(ConfigurePrimaryHttpMessageHandler)
            .AddPolicyHandler(_ => WaitAndRetryAsync);
    }

    [ExcludeFromCodeCoverage]
    public static IHttpClientBuilder AddHttpProxyClientWithRetry(this IServiceCollection services)
    {
        return services
            .AddHttpClient(ProxyClientWithRetry)
            .ConfigurePrimaryHttpMessageHandler(ConfigurePrimaryHttpMessageHandler)
            .AddPolicyHandler(_ => WaitAndRetryAsync);
    }

    [ExcludeFromCodeCoverage]
    public static IHttpClientBuilder AddDecisionComparerHttpProxyClientWithRetry(this IServiceCollection services)
    {
        services
            .AddOptions<DecisionComparerApiOptions>()
            .BindConfiguration(DecisionComparerApiOptions.SectionName)
            .ValidateDataAnnotations();

        var clientBuilder = services
            .AddHttpClient(DecisionComparerProxyClientWithRetry)
            .ConfigurePrimaryHttpMessageHandler(ConfigurePrimaryHttpMessageHandler)
            .ConfigureHttpClient(
                (sp, c) => sp.GetRequiredService<IOptions<DecisionComparerApiOptions>>().Value.Configure(c)
            )
            .AddHeaderPropagation();

        clientBuilder.AddStandardResilienceHandler(o =>
        {
            o.Retry.DisableFor(HttpMethod.Delete, HttpMethod.Post, HttpMethod.Connect, HttpMethod.Patch);
        });

        return clientBuilder;
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
