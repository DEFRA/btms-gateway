using System.Diagnostics.CodeAnalysis;
using BtmsGateway.Config;
using BtmsGateway.Middleware;
using BtmsGateway.Services.Metrics;
using BtmsGateway.Utils.Logging;
using Defra.TradeImportsDataApi.Domain.Events;
using SlimMessageBus.Host;
using SlimMessageBus.Host.AmazonSQS;
using SlimMessageBus.Host.Interceptor;
using SlimMessageBus.Host.Serialization.SystemTextJson;

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

        services.AddSingleton(typeof(IProducerInterceptor<>), typeof(TraceContextInterceptor<>));

        services.AddSlimMessageBus(messageBusBuilder =>
        {
            var awsSqsOptions = services
                .AddValidateOptions<AwsSqsOptions>(configuration, AwsSqsOptions.SectionName)
                .Get();
            messageBusBuilder.AddChildBus(
                "SQS_ResourceEvents",
                mbb =>
                {
                    mbb.AddAmazonConsumers(awsSqsOptions, configuration);
                }
            );

            messageBusBuilder.AddChildBus(
                "SQS_ActivityEvents",
                mbb =>
                {
                    mbb.WithProviderAmazonSQS(cfg =>
                    {
                        ////There is a bug: https://github.com/zarusz/SlimMessageBus/issues/443 that requires enabling topology provisioning even for producers
                        cfg.TopologyProvisioning.Enabled = true;
                        cfg.TopologyProvisioning.CanConsumerCreateTopic = false;
                        cfg.TopologyProvisioning.CanConsumerCreateQueue = false;
                        cfg.TopologyProvisioning.CanProducerCreateQueue = false;
                        cfg.TopologyProvisioning.CanProducerCreateTopic = false;
                        cfg.TopologyProvisioning.CanConsumerCreateTopicSubscription = false;
                        cfg.SnsClientProviderFactory = _ => new CdpCredentialsSnsClientProvider(
                            cfg.SnsClientConfig,
                            configuration
                        );
                    });

                    mbb.AddJsonSerializer();
                    mbb.WithSerializer<JsonMessageSerializer>();
                    mbb.Produce<BtmsActivityEvent<BtmsToCdsActivity>>(x =>
                        x.DefaultTopic(awsSqsOptions.ActivityEventsTopicName)
                    );
                }
            );
        });

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
