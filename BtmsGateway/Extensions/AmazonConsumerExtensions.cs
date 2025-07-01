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
            cfg.ClientProviderFactory = _ => new CdpCredentialsSqsClientProvider(cfg.SqsClientConfig, configuration);
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
                    .Queue(options.OutboundClearanceDecisionsQueueName)
                    .Instances(options.ConsumersPerHost)
            );
    }
}
