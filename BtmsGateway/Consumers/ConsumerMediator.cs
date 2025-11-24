using System.Text.Json;
using BtmsGateway.Config;
using BtmsGateway.Domain;
using BtmsGateway.Extensions;
using BtmsGateway.Services.Routing;
using BtmsGateway.Utils;
using BtmsGateway.Utils.Logging;
using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;
using Defra.TradeImportsDataApi.Domain.Events;
using Microsoft.Extensions.Options;
using SlimMessageBus;

namespace BtmsGateway.Consumers;

public class ConsumerMediator(
    IDecisionSender decisionSender,
    IErrorNotificationSender errorNotificationSender,
    ILoggerFactory loggerFactory,
    IOptions<CdsOptions> cdsOptions
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
            loggerFactory.CreateLogger<ClearanceDecisionConsumer>(),
            cdsOptions
        );

        return consumer.OnHandle(Deserialize<CustomsDeclarationEvent>(message), cancellationToken);
    }

    private Task HandleProcessingError(JsonElement message, CancellationToken cancellationToken)
    {
        var consumer = new ProcessingErrorConsumer(
            errorNotificationSender,
            loggerFactory.CreateLogger<ProcessingErrorConsumer>(),
            cdsOptions
        );

        return consumer.OnHandle(Deserialize<ProcessingErrorEvent>(message), cancellationToken);
    }

    private Task HandleUnknown(string resourceType)
    {
        _logger.LogWarning("No Consumer for Resource Type: {ResourceType}", resourceType);

        return Task.CompletedTask;
    }

    private static ResourceEvent<T> Deserialize<T>(JsonElement message) =>
        MessageDeserializer.Deserialize<ResourceEvent<T>>(message.ToString(), null)
        ?? throw new InvalidOperationException("Invalid message received from queue.");
}
