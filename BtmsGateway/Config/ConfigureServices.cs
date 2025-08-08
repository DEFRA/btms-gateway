using System.Diagnostics.CodeAnalysis;
using Amazon.SimpleNotificationService;
using BtmsGateway.Extensions;
using BtmsGateway.Services.Checking;
using BtmsGateway.Services.Metrics;
using BtmsGateway.Services.Routing;
using BtmsGateway.Utils.Http;
using FluentValidation;
using Microsoft.FeatureManagement;
using ILogger = Serilog.ILogger;

namespace BtmsGateway.Config;

public static class ConfigureServices
{
    public static IHttpClientBuilder? HttpRoutedClientWithRetryBuilder { get; private set; }
    public static IHttpClientBuilder? HttpForkedClientWithRetryBuilder { get; private set; }
    public static IHttpClientBuilder? HttpClientWithRetryBuilder { get; private set; }
    public static IHttpClientBuilder? DecisionComparerHttpClientWithRetryBuilder { get; private set; }

    [ExcludeFromCodeCoverage]
    public static void AddServices(this WebApplicationBuilder builder, ILogger? logger = null)
    {
        if (logger != null)
        {
            // This is added to support end to end tests that boostrap their own configuration
            // making use of common methods such as this to add the application services. The
            // end to end tests need to be reworked so they use an actual BTMS gateway host
            // running in Docker.
            builder.Services.AddSingleton(_ => logger);
        }

        builder.Services.AddHttpLogging(o =>
        {
            o.RequestHeaders.Add("X-cdp-request-id");
            o.RequestHeaders.Add("X-Amzn-Trace-Id");
        });

        var httpClientTimeoutSeconds = builder.Configuration.GetValue(
            "HttpClientTimeoutSeconds",
            Proxy.DefaultHttpClientTimeoutSeconds
        );
        var cdsHttpClientRetries = builder.Configuration.GetValue(
            "CdsHttpClientRetries",
            Proxy.DefaultCdsHttpClientRetries
        );

        HttpRoutedClientWithRetryBuilder = builder.Services.AddHttpProxyRoutedClientWithRetry(httpClientTimeoutSeconds);
        HttpForkedClientWithRetryBuilder = builder.Services.AddHttpProxyForkedClientWithRetry(httpClientTimeoutSeconds);
        HttpClientWithRetryBuilder = builder.Services.AddHttpProxyClientWithRetry(
            httpClientTimeoutSeconds,
            cdsHttpClientRetries
        );
        DecisionComparerHttpClientWithRetryBuilder = builder.Services.AddDecisionComparerHttpProxyClientWithRetry(
            httpClientTimeoutSeconds
        );

        builder.Services.AddHttpProxyClientWithoutRetry();
        builder.Services.AddDataApiHttpClient();

        builder.Services.AddDefaultAWSOptions(builder.Configuration.GetAWSOptions());
        builder.Services.AddAWSService<IAmazonSimpleNotificationService>();

        builder.Services.AddConsumers(builder.Configuration);

        builder.Services.AddValidatorsFromAssemblyContaining<Program>();

        builder.Services.AddSingleton<IQueueSender, QueueSender>();
        builder.Services.AddSingleton<IApiSender, ApiSender>();
        builder.Services.AddSingleton<IMessageRouter, MessageRouter>();
        builder.Services.AddSingleton<IMessageRoutes, MessageRoutes>();
        builder.Services.AddSingleton<IProcessRunner, ProcessRunner>();
        builder.Services.AddSingleton<CheckRoutes>();
        builder.Services.AddSingleton<MetricsHost>();
        builder.Services.AddSingleton<IDecisionSender, DecisionSender>();
        builder.Services.AddSingleton<IErrorNotificationSender, ErrorNotificationSender>();

        builder.Services.AddFeatureManagement(builder.Configuration.GetSection("FeatureFlags"));

        builder.Services.AddOptions<CdsOptions>().BindConfiguration(CdsOptions.SectionName).ValidateDataAnnotations();
    }
}
