using System.Diagnostics.CodeAnalysis;
using Amazon.SimpleNotificationService;
using BtmsGateway.Extensions;
using BtmsGateway.Services.Checking;
using BtmsGateway.Services.Metrics;
using BtmsGateway.Services.Routing;
using BtmsGateway.Utils.Http;
using FluentValidation;
using Microsoft.FeatureManagement;

namespace BtmsGateway.Config;

public static class ConfigureServices
{
    public static IHttpClientBuilder? HttpRoutedClientWithRetryBuilder { get; private set; }
    public static IHttpClientBuilder? HttpForkedClientWithRetryBuilder { get; private set; }
    public static IHttpClientBuilder? HttpClientWithRetryBuilder { get; private set; }
    public static IHttpClientBuilder? DecisionComparerHttpClientWithRetryBuilder { get; private set; }

    [ExcludeFromCodeCoverage]
    public static void AddServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddHttpLogging(o =>
        {
            o.RequestHeaders.Add("X-cdp-request-id");
            o.RequestHeaders.Add("X-Amzn-Trace-Id");
        });
        HttpRoutedClientWithRetryBuilder = builder.Services.AddHttpProxyRoutedClientWithRetry();
        HttpForkedClientWithRetryBuilder = builder.Services.AddHttpProxyForkedClientWithRetry();
        HttpClientWithRetryBuilder = builder.Services.AddHttpProxyClientWithRetry();
        DecisionComparerHttpClientWithRetryBuilder = builder.Services.AddDecisionComparerHttpProxyClientWithRetry();

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
    }
}
