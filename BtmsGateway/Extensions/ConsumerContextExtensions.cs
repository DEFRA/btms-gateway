using SlimMessageBus;

namespace BtmsGateway.Extensions;

public static class MessageBusHeaders
{
    public const string ResourceType = nameof(ResourceType);
}

public static class ConsumerContextExtensions
{
    public static string GetResourceType(this IConsumerContext consumerContext)
    {
        if (consumerContext.Headers.TryGetValue(MessageBusHeaders.ResourceType, out var value))
        {
            return value.ToString()!;
        }

        return string.Empty;
    }
}
