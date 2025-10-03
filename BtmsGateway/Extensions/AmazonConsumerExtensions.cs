using BtmsGateway.Config;
using BtmsGateway.Consumers;
using BtmsGateway.Utils;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SlimMessageBus.Host;
using SlimMessageBus.Host.AmazonSQS;
using SlimMessageBus.Host.Serialization;

namespace BtmsGateway.Extensions;

public static class AmazonConsumerExtensions
{
    public static void AddAmazonConsumers(
        this MessageBusBuilder messageBusBuilder,
        AwsSqsOptions options,
        IConfiguration configuration
    )
    {
        messageBusBuilder.AddServicesFromAssemblyContaining<ClearanceDecisionConsumer>();

        messageBusBuilder.WithProviderAmazonSQS(cfg =>
        {
            cfg.TopologyProvisioning.Enabled = false;
            cfg.SqsClientProviderFactory = _ => new CdpCredentialsSqsClientProvider(cfg.SqsClientConfig, configuration);
            cfg.SnsClientProviderFactory = _ => new CdpCredentialsSnsClientProvider(cfg.SnsClientConfig, configuration);
        });

        messageBusBuilder.RegisterSerializer<ToStringSerializer>(s =>
        {
            s.TryAddSingleton(_ => new ToStringSerializer());
            s.TryAddSingleton<IMessageSerializer<string>>(svp => svp.GetRequiredService<ToStringSerializer>());
        });

        messageBusBuilder
            .AutoStartConsumersEnabled(options.AutoStartConsumers)
            .Consume<string>(x =>
                x.WithConsumer<ConsumerMediator>()
                    .Queue(options.ResourceEventsQueueName)
                    .Instances(options.ConsumersPerHost)
                    .VisibilityTimeout(options.VisibilityTimeout)
            );
    }
}
