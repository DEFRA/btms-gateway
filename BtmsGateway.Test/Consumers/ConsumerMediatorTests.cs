using System.Text.Json;
using BtmsGateway.Consumers;
using BtmsGateway.Exceptions;
using BtmsGateway.Extensions;
using BtmsGateway.Services.Routing;
using Defra.TradeImportsDataApi.Api.Client;
using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;
using Defra.TradeImportsDataApi.Domain.Events;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SlimMessageBus;

namespace BtmsGateway.Test.Consumers;

public class ConsumerMediatorTests
{
    [Fact]
    public async Task WhenCustomsDeclaration_ShouldPassThroughToClearanceDecisionConsumer()
    {
        var context = Substitute.For<IConsumerContext>();
        context.Headers.Returns(
            new Dictionary<string, object>
            {
                { MessageBusHeaders.ResourceType, ResourceEventResourceTypes.CustomsDeclaration },
            }
        );
        var subject = new ConsumerMediator(
            Substitute.For<ITradeImportsDataApiClient>(),
            Substitute.For<IDecisionSender>(),
            Substitute.For<ILoggerFactory>()
        )
        {
            Context = context,
        };

        var message = JsonSerializer.Deserialize<JsonElement>(
            JsonSerializer.Serialize(
                new ResourceEvent<CustomsDeclaration>
                {
                    ResourceId = "mrn",
                    ResourceType = ResourceEventResourceTypes.CustomsDeclaration,
                    Operation = ResourceEventOperations.Created,
                    SubResourceType = ResourceEventSubResourceTypes.ClearanceDecision,
                }
            )
        );

        var act = async () => await subject.OnHandle(message, CancellationToken.None);

        await act.Should().ThrowAsync<ClearanceDecisionProcessingException>();
    }

    [Fact]
    public async Task WhenProcessingError_ShouldNotThrow()
    {
        var context = Substitute.For<IConsumerContext>();
        context.Headers.Returns(
            new Dictionary<string, object>
            {
                { MessageBusHeaders.ResourceType, ResourceEventResourceTypes.ProcessingError },
            }
        );
        var subject = new ConsumerMediator(
            Substitute.For<ITradeImportsDataApiClient>(),
            Substitute.For<IDecisionSender>(),
            Substitute.For<ILoggerFactory>()
        )
        {
            Context = context,
        };

        var message = JsonSerializer.Deserialize<JsonElement>(
            JsonSerializer.Serialize(
                new ResourceEvent<CustomsDeclaration>
                {
                    ResourceId = "mrn",
                    ResourceType = ResourceEventResourceTypes.ProcessingError,
                    Operation = ResourceEventOperations.Created,
                }
            )
        );

        var act = async () => await subject.OnHandle(message, CancellationToken.None);

        await act.Should().NotThrowAsync<ClearanceDecisionProcessingException>();
    }

    [Fact]
    public async Task WhenUnsupportedResourceType_ShouldNotThrow()
    {
        var context = Substitute.For<IConsumerContext>();
        context.Headers.Returns(
            new Dictionary<string, object>
            {
                { MessageBusHeaders.ResourceType, ResourceEventResourceTypes.ImportPreNotification },
            }
        );
        var subject = new ConsumerMediator(
            Substitute.For<ITradeImportsDataApiClient>(),
            Substitute.For<IDecisionSender>(),
            Substitute.For<ILoggerFactory>()
        )
        {
            Context = context,
        };

        var message = JsonSerializer.Deserialize<JsonElement>(
            JsonSerializer.Serialize(
                new ResourceEvent<CustomsDeclaration>
                {
                    ResourceId = "mrn",
                    ResourceType = ResourceEventResourceTypes.ImportPreNotification,
                    Operation = ResourceEventOperations.Created,
                }
            )
        );

        var act = async () => await subject.OnHandle(message, CancellationToken.None);

        await act.Should().NotThrowAsync<ClearanceDecisionProcessingException>();
    }
}
