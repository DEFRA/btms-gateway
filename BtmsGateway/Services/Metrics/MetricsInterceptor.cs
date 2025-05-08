using BtmsGateway.Config;
using BtmsGateway.Extensions;
using Defra.TradeImportsDataApi.Domain.Events;
using Microsoft.Extensions.Options;
using SlimMessageBus;
using SlimMessageBus.Host.Interceptor;

namespace BtmsGateway.Services.Metrics;

public class MetricsInterceptor<TMessage>(
    IConsumerMetrics consumerMetrics,
    IRequestMetrics requestMetrics,
    IOptions<AwsSqsOptions> awsSqsOptions
) : IConsumerInterceptor<TMessage>
    where TMessage : notnull
{
    public async Task<object> OnHandle(TMessage message, Func<Task<object>> next, IConsumerContext context)
    {
        var startingTimestamp = TimeProvider.System.GetTimestamp();
        var resourceType = context.GetResourceType();
        var subResourceType = context.GetSubResourceType();

        try
        {
            if (
                string.Equals(
                    subResourceType,
                    ResourceEventSubResourceTypes.ClearanceDecision,
                    StringComparison.InvariantCultureIgnoreCase
                )
            )
            {
                requestMetrics.MessageReceived(
                    subResourceType,
                    awsSqsOptions.Value.OutboundClearanceDecisionsQueueName,
                    "BTMS Decision",
                    "Consumer"
                );
            }

            consumerMetrics.Start(context.Path, context.Consumer.GetType().Name, resourceType, subResourceType);
            return await next();
        }
        catch (Exception exception)
        {
            consumerMetrics.Faulted(
                context.Path,
                context.Consumer.GetType().Name,
                resourceType,
                subResourceType,
                exception
            );
            throw;
        }
        finally
        {
            consumerMetrics.Complete(
                context.Path,
                context.Consumer.GetType().Name,
                TimeProvider.System.GetElapsedTime(startingTimestamp).TotalMilliseconds,
                resourceType,
                subResourceType
            );
        }
    }
}
