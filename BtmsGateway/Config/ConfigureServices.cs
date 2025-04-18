using System.Diagnostics.CodeAnalysis;
using Amazon.SimpleNotificationService;
using BtmsGateway.Services.Checking;
using BtmsGateway.Services.Metrics;
using BtmsGateway.Services.Routing;
using BtmsGateway.Utils.Http;
using FluentValidation;
using ILogger = Serilog.ILogger;

namespace BtmsGateway.Config;

public static class ConfigureServices
{
    public static IHttpClientBuilder? HttpRoutedClientWithRetryBuilder { get; private set; }
    public static IHttpClientBuilder? HttpForkedClientWithRetryBuilder { get; private set; }

    [ExcludeFromCodeCoverage]
    public static void AddServices(this WebApplicationBuilder builder, ILogger logger)
    {
        builder.Services.AddSingleton(logger);
        builder.Services.AddHttpLogging(o =>
        {
            o.RequestHeaders.Add("X-cdp-request-id");
            o.RequestHeaders.Add("X-Amzn-Trace-Id");
        });
        HttpRoutedClientWithRetryBuilder = builder.Services.AddHttpProxyRoutedClientWithRetry(logger);
        HttpForkedClientWithRetryBuilder = builder.Services.AddHttpProxyForkedClientWithRetry(logger);
        builder.Services.AddHttpProxyClientWithoutRetry(logger);

        builder.Services.AddDefaultAWSOptions(builder.Configuration.GetAWSOptions());
        builder.Services.AddAWSService<IAmazonSimpleNotificationService>();

        builder.Services.AddValidatorsFromAssemblyContaining<Program>();

        builder.Services.AddSingleton<IQueueSender, QueueSender>();
        builder.Services.AddSingleton<IApiSender, ApiSender>();
        builder.Services.AddSingleton<IMessageRouter, MessageRouter>();
        builder.Services.AddSingleton<IMessageRoutes, MessageRoutes>();
        builder.Services.AddSingleton<IProcessRunner, ProcessRunner>();
        builder.Services.AddSingleton<CheckRoutes>();
        builder.Services.AddSingleton<MetricsHost>();
    }
}