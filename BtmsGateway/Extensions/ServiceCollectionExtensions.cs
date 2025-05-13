using System.Diagnostics.CodeAnalysis;
using BtmsGateway.Config;
using BtmsGateway.Services.Metrics;
using BtmsGateway.Utils.Logging;
using Defra.TradeImportsDataApi.Api.Client;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
using SlimMessageBus.Host;
using SlimMessageBus.Host.Interceptor;

namespace BtmsGateway.Extensions;

[ExcludeFromCodeCoverage]
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddConsumers(this IServiceCollection services, IConfiguration configuration)
    {
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
        services.AddOptions<DataApiOptions>().BindConfiguration(DataApiOptions.SectionName).ValidateDataAnnotations();

        services
            .AddTradeImportsDataApiClient()
            .ConfigureHttpClient((sp, c) => sp.GetRequiredService<IOptions<DataApiOptions>>().Value.Configure(c))
            .AddHeaderPropagation()
            .AddStandardResilienceHandler(o =>
            {
                o.Retry.DisableForUnsafeHttpMethods();
            });

        return services;
    }

    public static IServiceCollection AddTracingForConsumers(this IServiceCollection services)
    {
        services.AddScoped(typeof(IConsumerInterceptor<>), typeof(TraceContextInterceptor<>));

        return services;
    }

    public static IHttpContextAccessor GetHttpContextAccessor(this IServiceCollection services)
    {
        return services.BuildServiceProvider().GetRequiredService<IHttpContextAccessor>();
    }

    public static IServiceCollection AddOperationalMetrics(this IServiceCollection services)
    {
        services.AddSingleton<IRequestMetrics, RequestMetrics>();
        services.AddSingleton<IConsumerMetrics, ConsumerMetrics>();
        services.AddSingleton(typeof(IConsumerInterceptor<>), typeof(MetricsInterceptor<>));

        return services;
    }
}
