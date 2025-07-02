using System.Text.Json;
using BtmsGateway.Domain;
using BtmsGateway.Extensions;
using BtmsGateway.Services.Routing;
using BtmsGateway.Utils;
using BtmsGateway.Utils.Logging;
using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;
using Defra.TradeImportsDataApi.Domain.Events;
using SlimMessageBus;

namespace BtmsGateway.Consumers;

public class ConsumerMediator(
    IDecisionSender decisionSender,
    IErrorNotificationSender errorNotificationSender,
    ILoggerFactory loggerFactory
) : IConsumer<string>, IConsumerWithContext
{
    private readonly ILogger<ConsumerMediator> _logger = loggerFactory.CreateLogger<ConsumerMediator>();

    public IConsumerContext Context { get; set; } = null!;

    public Task OnHandle(string received, CancellationToken cancellationToken)
    {
        var message = MessageDeserializer.Deserialize<JsonElement>(received, Context.Headers.GetContentEncoding());

        var resourceType = Context.GetResourceType();

        return resourceType switch
        {
            ResourceEventResourceTypes.CustomsDeclaration => HandleCustomsDeclaration(message, cancellationToken),
            ResourceEventResourceTypes.ProcessingError => HandleProcessingError(message, cancellationToken),
            _ => HandleUnknown(resourceType),
        };
    }

    private Task HandleCustomsDeclaration(JsonElement message, CancellationToken cancellationToken)
    {
        var consumer = new ClearanceDecisionConsumer(
            decisionSender,
            loggerFactory.CreateLogger<ClearanceDecisionConsumer>()
        );

        return consumer.OnHandle(Deserialize<CustomsDeclaration>(message), cancellationToken);
    }

    private Task HandleProcessingError(JsonElement message, CancellationToken cancellationToken)
    {
        var consumer = new ProcessingErrorConsumer(
            errorNotificationSender,
            loggerFactory.CreateLogger<ProcessingErrorConsumer>()
        );

        return consumer.OnHandle(Deserialize<ProcessingErrorResource>(message), cancellationToken);
    }

    private Task HandleUnknown(string resourceType)
    {
        _logger.LogWarning("No Consumer for Resource Type: {ResourceType}", resourceType);

        return Task.CompletedTask;
    }

    private static ResourceEvent<T> Deserialize<T>(JsonElement message) =>
        message.Deserialize<ResourceEvent<T>>()
        ?? throw new InvalidOperationException("Invalid message received from queue.");
}
