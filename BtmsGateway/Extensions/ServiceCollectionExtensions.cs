using System.Diagnostics.CodeAnalysis;
using BtmsGateway.Config;
using BtmsGateway.Middleware;
using BtmsGateway.Services.Metrics;
using BtmsGateway.Utils.Logging;
using Defra.TradeImportsDataApi.Api.Client;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
using Polly;
using SlimMessageBus.Host;
using SlimMessageBus.Host.Interceptor;

namespace BtmsGateway.Extensions;

[ExcludeFromCodeCoverage]
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddConsumers(this IServiceCollection services, IConfiguration configuration)
    {
        // Order of interceptors is important here
        services.AddSingleton(typeof(IConsumerInterceptor<>), typeof(TraceContextInterceptor<>));
        services.AddSingleton(typeof(IConsumerInterceptor<>), typeof(LoggingInterceptor<>));
        services.AddSingleton<IConsumerMetrics, ConsumerMetrics>();
        services.AddSingleton(typeof(IConsumerInterceptor<>), typeof(MetricsInterceptor<>));

        services.AddSlimMessageBus(messageBusBuilder =>
        {
            var awsSqsOptions = services
                .AddValidateOptions<AwsSqsOptions>(configuration, AwsSqsOptions.SectionName)
                .Get();
            messageBusBuilder.AddAmazonConsumers(awsSqsOptions, configuration);
        });

        return services;
    }

    public static IServiceCollection AddDataApiHttpClient(this IServiceCollection services)
    {
        var resilienceOptions = new HttpStandardResilienceOptions { Retry = { UseJitter = true } };
        resilienceOptions.Retry.DisableForUnsafeHttpMethods();

        services.AddOptions<DataApiOptions>().BindConfiguration(DataApiOptions.SectionName).ValidateDataAnnotations();

        services
            .AddTradeImportsDataApiClient()
            .ConfigureHttpClient(
                (sp, c) =>
                {
                    sp.GetRequiredService<IOptions<DataApiOptions>>().Value.Configure(c);

                    // Disable the HttpClient timeout to allow the resilient pipeline below
                    // to handle all timeouts
                    c.Timeout = Timeout.InfiniteTimeSpan;
                }
            )
            .AddHeaderPropagation()
            .AddResilienceHandler(
                "DataApi",
                builder =>
                {
                    builder
                        .AddTimeout(resilienceOptions.TotalRequestTimeout)
                        .AddRetry(resilienceOptions.Retry)
                        .AddTimeout(resilienceOptions.AttemptTimeout);
                }
            );

        return services;
    }

    public static IServiceCollection AddOperationalMetrics(this IServiceCollection services)
    {
        services.AddSingleton<IRequestMetrics, RequestMetrics>();
        services.AddSingleton<IHealthMetrics, HealthMetrics>();
        services.AddTransient<MetricsMiddleware>();

        return services;
    }
}
