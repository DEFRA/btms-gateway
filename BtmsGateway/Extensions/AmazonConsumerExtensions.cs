using System.Text.Json;
using BtmsGateway.Config;
using BtmsGateway.Consumers;
using SlimMessageBus.Host;
using SlimMessageBus.Host.AmazonSQS;
using SlimMessageBus.Host.Serialization.SystemTextJson;

namespace BtmsGateway.Extensions;

public static class AmazonConsumerExtensions
{
    public static void AddAmazonConsumers(
        this MessageBusBuilder messageBusBuilder,
        AwsSqsOptions options,
        IConfiguration configuration
    )
    {
        messageBusBuilder
            .AddServicesFromAssemblyContaining<ClearanceDecisionConsumer>(consumerLifetime: ServiceLifetime.Scoped)
            .PerMessageScopeEnabled();

        messageBusBuilder.WithProviderAmazonSQS(cfg =>
        {
            cfg.TopologyProvisioning.Enabled = false;
            cfg.ClientProviderFactory = _ => new CdpCredentialsSqsClientProvider(cfg.SqsClientConfig, configuration);
        });

        messageBusBuilder.AddJsonSerializer();

        messageBusBuilder.Consume<JsonElement>(x =>
            x.WithConsumer<ConsumerMediator>()
                .Queue(options.OutboundClearanceDecisionsQueueName)
                .Instances(options.ConsumersPerHost)
        );
    }
}
