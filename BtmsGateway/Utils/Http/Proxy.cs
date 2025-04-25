using System.Net;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
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
    public static IHttpClientBuilder AddHttpProxyClientWithoutRetry(this IServiceCollection services, Serilog.ILogger logger)
    {
        return services.AddHttpClient(ProxyClientWithoutRetry).ConfigurePrimaryHttpMessageHandler(() => ConfigurePrimaryHttpMessageHandler(logger));
    }

    private static readonly AsyncRetryPolicy<HttpResponseMessage> WaitAndRetryAsync = HttpPolicyExtensions.HandleTransientHttpError().WaitAndRetryAsync(3, _ => TimeSpan.FromMilliseconds(1000));

    [ExcludeFromCodeCoverage]
    public static IHttpClientBuilder AddHttpProxyRoutedClientWithRetry(this IServiceCollection services, Serilog.ILogger logger)
    {
        return services.AddHttpClient(RoutedClientWithRetry).ConfigurePrimaryHttpMessageHandler(() => ConfigurePrimaryHttpMessageHandler(logger)).AddPolicyHandler(_ => WaitAndRetryAsync);
    }

    [ExcludeFromCodeCoverage]
    public static IHttpClientBuilder AddHttpProxyForkedClientWithRetry(this IServiceCollection services, Serilog.ILogger logger)
    {
        return services.AddHttpClient(ForkedClientWithRetry).ConfigurePrimaryHttpMessageHandler(() => ConfigurePrimaryHttpMessageHandler(logger)).AddPolicyHandler(_ => WaitAndRetryAsync);
    }

    [ExcludeFromCodeCoverage]
    public static IHttpClientBuilder AddHttpProxyClientWithRetry(this IServiceCollection services, Serilog.ILogger logger)
    {
        return services.AddHttpClient(ProxyClientWithRetry).ConfigurePrimaryHttpMessageHandler(() => ConfigurePrimaryHttpMessageHandler(logger)).AddPolicyHandler(_ => WaitAndRetryAsync);
    }

    [ExcludeFromCodeCoverage]
    public static IHttpClientBuilder AddDecisionComparerHttpProxyClientWithRetry(this IServiceCollection services, Serilog.ILogger logger)
    {
        services.AddOptions<DecisionComparerApiOptions>().BindConfiguration(DecisionComparerApiOptions.SectionName).ValidateDataAnnotations();

        var clientBuilder = services.AddHttpClient(DecisionComparerProxyClientWithRetry)
            .ConfigurePrimaryHttpMessageHandler(() => ConfigurePrimaryHttpMessageHandler(logger))
            .ConfigureHttpClient(
                (sp, c) =>
                {
                    var options = sp.GetRequiredService<IOptions<DecisionComparerApiOptions>>().Value;
                    c.BaseAddress = new Uri(options.BaseAddress);

                    if (options.BasicAuthCredential != null)
                        c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                            "Basic",
                            options.BasicAuthCredential
                        );

                    if (c.BaseAddress.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase))
                        c.DefaultRequestVersion = HttpVersion.Version20;
                })
            .AddHeaderPropagation();

        clientBuilder.AddStandardResilienceHandler(o =>
        {
            o.Retry.DisableFor(HttpMethod.Delete, HttpMethod.Post, HttpMethod.Connect, HttpMethod.Patch);
        });

        return clientBuilder;
    }

    [ExcludeFromCodeCoverage]
    public static HttpClientHandler ConfigurePrimaryHttpMessageHandler(Serilog.ILogger logger)
    {
        var proxyUri = Environment.GetEnvironmentVariable("CDP_HTTPS_PROXY");
        return CreateHttpClientHandler(proxyUri, logger);
    }

    public static HttpClientHandler CreateHttpClientHandler(string? proxyUri, Serilog.ILogger logger)
    {
        var proxy = CreateProxy(proxyUri, logger);
        return new HttpClientHandler { Proxy = proxy, UseProxy = proxyUri != null };
    }

    public static WebProxy CreateProxy(string? proxyUri, Serilog.ILogger logger)
    {
        var proxy = new WebProxy
        {
            BypassProxyOnLocal = true
        };
        if (proxyUri != null)
        {
            ConfigureProxy(proxy, proxyUri, logger);
        }
        else
        {
            logger.Warning("CDP_HTTP_PROXY is NOT set, proxy client will be disabled");
        }
        return proxy;
    }

    public static void ConfigureProxy(WebProxy proxy, string proxyUri, Serilog.ILogger logger)
    {
        logger.Debug("Creating proxy http client");
        var uri = new UriBuilder(proxyUri);

        var credentials = GetCredentialsFromUri(uri);
        if (credentials != null)
        {
            logger.Debug("Setting proxy credentials");
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
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password)) return null;
        return new NetworkCredential(username, password);
    }

}