using System.Text.Json;
using BtmsGateway.Extensions;
using BtmsGateway.Services.Routing;
using Defra.TradeImportsDataApi.Api.Client;
using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;
using Defra.TradeImportsDataApi.Domain.Events;
using Defra.TradeImportsDataApi.Domain.ProcessingErrors;
using SlimMessageBus;

namespace BtmsGateway.Consumers;

public class ConsumerMediator(
    ITradeImportsDataApiClient api,
    IDecisionSender decisionSender,
    ILoggerFactory loggerFactory
) : IConsumer<JsonElement>, IConsumerWithContext
{
    private readonly ILogger<ConsumerMediator> _logger = loggerFactory.CreateLogger<ConsumerMediator>();

    public IConsumerContext Context { get; set; } = null!;

    public Task OnHandle(JsonElement message, CancellationToken cancellationToken)
    {
        var resourceType = Context.GetResourceType();

        return resourceType switch
        {
            ResourceEventResourceTypes.CustomsDeclaration => HandleCustomsDeclaration(message, cancellationToken),
            ResourceEventResourceTypes.ProcessingError => HandleProcessingError(message),
            _ => HandleUnknown(resourceType),
        };
    }

    private Task HandleCustomsDeclaration(JsonElement message, CancellationToken cancellationToken)
    {
        var consumer = new ClearanceDecisionConsumer(
            api,
            decisionSender,
            loggerFactory.CreateLogger<ClearanceDecisionConsumer>()
        );

        return consumer.OnHandle(Deserialize<CustomsDeclaration>(message), cancellationToken);
    }

    private Task HandleProcessingError(JsonElement message)
    {
        var @event = Deserialize<ProcessingError>(message);

        _logger.LogInformation("Processing Error Resource Event received from queue: {ProcessingError}", @event);

        return Task.CompletedTask;
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
