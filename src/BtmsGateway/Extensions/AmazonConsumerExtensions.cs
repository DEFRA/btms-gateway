using System.Text.Json;
using BtmsGateway.Config;
using BtmsGateway.Consumers;
using Defra.TradeImports.SMB.CompressedSerializer;
using Defra.TradeImportsDataApi.Domain.Events;
using SlimMessageBus.Host;
using SlimMessageBus.Host.AmazonSQS;

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

        messageBusBuilder
            .AddCompressedJsonSerializer(
                new() { PropertyNameCaseInsensitive = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase }
            )
            .AutoStartConsumersEnabled(options.AutoStartConsumers)
            .Consume<ResourceEvent<CustomsDeclarationEvent>>(x =>
                x.WithConsumer<ClearanceDecisionConsumer>()
                    .Queue(options.ResourceEventsQueueName)
                    .Instances(options.ConsumersPerHost)
                    .VisibilityTimeout(options.VisibilityTimeout)
                    .SkipUndeclaredMessageTypes()
                    .FilterOnResourceTypeHeader(ResourceEventResourceTypes.CustomsDeclaration)
            )
            .Consume<ResourceEvent<ProcessingErrorEvent>>(x =>
                x.WithConsumer<ProcessingErrorConsumer>()
                    .Queue(options.ResourceEventsQueueName)
                    .Instances(options.ConsumersPerHost)
                    .VisibilityTimeout(options.VisibilityTimeout)
                    .SkipUndeclaredMessageTypes()
                    .FilterOnResourceTypeHeader(ResourceEventResourceTypes.ProcessingError)
            );
    }
}
