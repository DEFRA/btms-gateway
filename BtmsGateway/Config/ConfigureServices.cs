using System.Diagnostics.CodeAnalysis;
using Amazon.SimpleNotificationService;
using Amazon.SQS;
using BtmsGateway.Extensions;
using BtmsGateway.Services.Admin;
using BtmsGateway.Services.Checking;
using BtmsGateway.Services.Metrics;
using BtmsGateway.Services.Routing;
using BtmsGateway.Utils.Http;
using Elastic.CommonSchema;
using FluentValidation;

namespace BtmsGateway.Config;

public static class ConfigureServices
{
    public static IHttpClientBuilder? HttpRoutedClientWithRetryBuilder { get; private set; }
    public static IHttpClientBuilder? HttpClientWithRetryBuilder { get; private set; }

    [ExcludeFromCodeCoverage]
    public static void AddServices(this WebApplicationBuilder builder)
    {
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

        builder.Services.AddTransient<ProxyLoggingHandler>();

        HttpRoutedClientWithRetryBuilder = builder.Services.AddHttpProxyRoutedClientWithRetry(httpClientTimeoutSeconds);
        HttpClientWithRetryBuilder = builder.Services.AddHttpProxyClientWithRetry(
            httpClientTimeoutSeconds,
            cdsHttpClientRetries
        );

        builder.Services.AddHttpProxyClientWithoutRetry();

        builder.Services.AddDefaultAWSOptions(builder.Configuration.GetAWSOptions());
        builder.Services.AddAWSService<IAmazonSimpleNotificationService>();
        builder.Services.AddAWSService<IAmazonSQS>();

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
        builder.Services.AddSingleton<IResourceEventsDeadLetterService, ResourceEventsDeadLetterService>();

        builder.Services.AddOptions<CdsOptions>().BindConfiguration(CdsOptions.SectionName).ValidateDataAnnotations();
    }
}
