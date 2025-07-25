using System.IO.Compression;
using System.Text;
using System.Text.Json;
using BtmsGateway.Config;
using BtmsGateway.Consumers;
using BtmsGateway.Domain;
using BtmsGateway.Exceptions;
using BtmsGateway.Extensions;
using BtmsGateway.Services.Routing;
using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;
using Defra.TradeImportsDataApi.Domain.Errors;
using Defra.TradeImportsDataApi.Domain.Events;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
            Substitute.For<IDecisionSender>(),
            Substitute.For<IErrorNotificationSender>(),
            Substitute.For<ILoggerFactory>(),
            Substitute.For<IOptions<CdsOptions>>()
        )
        {
            Context = context,
        };

        var message = JsonSerializer.Serialize(
            new ResourceEvent<CustomsDeclaration>
            {
                ResourceId = "mrn",
                ResourceType = ResourceEventResourceTypes.CustomsDeclaration,
                Operation = ResourceEventOperations.Created,
                SubResourceType = ResourceEventSubResourceTypes.ClearanceDecision,
            }
        );

        var act = async () => await subject.OnHandle(message, CancellationToken.None);

        await act.Should().ThrowAsync<ClearanceDecisionProcessingException>();
    }

    [Fact]
    public async Task WhenProcessingError_ShouldPassThroughToProcessingErrorConsumer()
    {
        var context = Substitute.For<IConsumerContext>();
        context.Headers.Returns(
            new Dictionary<string, object>
            {
                { MessageBusHeaders.ResourceType, ResourceEventResourceTypes.ProcessingError },
            }
        );
        var subject = new ConsumerMediator(
            Substitute.For<IDecisionSender>(),
            Substitute.For<IErrorNotificationSender>(),
            Substitute.For<ILoggerFactory>(),
            Substitute.For<IOptions<CdsOptions>>()
        )
        {
            Context = context,
        };

        var message = JsonSerializer.Serialize(
            new ResourceEvent<ProcessingErrorResource>
            {
                ResourceId = "mrn",
                ResourceType = ResourceEventResourceTypes.ProcessingError,
                Operation = ResourceEventOperations.Created,
                Resource = new ProcessingErrorResource { ProcessingErrors = [new ProcessingError()] },
            }
        );

        var act = async () => await subject.OnHandle(message, CancellationToken.None);

        await act.Should().ThrowAsync<ProcessingErrorProcessingException>();
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
            Substitute.For<IDecisionSender>(),
            Substitute.For<IErrorNotificationSender>(),
            Substitute.For<ILoggerFactory>(),
            Substitute.For<IOptions<CdsOptions>>()
        )
        {
            Context = context,
        };

        var message = JsonSerializer.Serialize(
            new ResourceEvent<CustomsDeclaration>
            {
                ResourceId = "mrn",
                ResourceType = ResourceEventResourceTypes.ImportPreNotification,
                Operation = ResourceEventOperations.Created,
            }
        );

        var act = async () => await subject.OnHandle(message, CancellationToken.None);

        await act.Should().NotThrowAsync<ClearanceDecisionProcessingException>();
    }

    [Fact]
    public async Task WhenCompressedMessage_ShouldPassThroughToConsumer()
    {
        var context = Substitute.For<IConsumerContext>();
        context.Headers.Returns(
            new Dictionary<string, object>
            {
                { MessageBusHeaders.ResourceType, ResourceEventResourceTypes.CustomsDeclaration },
                { MessageBusHeaders.ContentEncoding, "gzip, base64" },
            }
        );
        var subject = new ConsumerMediator(
            Substitute.For<IDecisionSender>(),
            Substitute.For<IErrorNotificationSender>(),
            Substitute.For<ILoggerFactory>(),
            Substitute.For<IOptions<CdsOptions>>()
        )
        {
            Context = context,
        };

        var message = JsonSerializer.Serialize(
            new ResourceEvent<CustomsDeclaration>
            {
                ResourceId = "mrn",
                ResourceType = ResourceEventResourceTypes.CustomsDeclaration,
                Operation = ResourceEventOperations.Created,
                SubResourceType = ResourceEventSubResourceTypes.ClearanceDecision,
            }
        );

        var act = async () => await subject.OnHandle(CompressMessage(message), CancellationToken.None);

        await act.Should().ThrowAsync<ClearanceDecisionProcessingException>();
    }

    private static string CompressMessage(string message)
    {
        var buffer = Encoding.UTF8.GetBytes(message);
        var memoryStream = new MemoryStream();
        using var gzipStream = new GZipStream(memoryStream, CompressionLevel.Optimal);
        gzipStream.Write(buffer, 0, buffer.Length);
        gzipStream.Flush();

        return Convert.ToBase64String(memoryStream.ToArray());
    }
}
