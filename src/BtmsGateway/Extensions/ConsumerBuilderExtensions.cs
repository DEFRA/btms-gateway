using SlimMessageBus.Host;

namespace BtmsGateway.Extensions;

public static class ConsumerBuilderExtensions
{
    public static AbstractConsumerBuilder<T> FilterOnResourceTypeHeader<T>(
        this AbstractConsumerBuilder<T> builder,
        string headerValue
    )
        where T : AbstractConsumerBuilder<T>
    {
        return builder.Filter(
            (headers, message) =>
                headers != null && headers.TryGetValue("ResourceType", out var v) && (string)v == headerValue
        );
    }

    public static AbstractConsumerBuilder<T> SkipUndeclaredMessageTypes<T>(this AbstractConsumerBuilder<T> builder)
        where T : AbstractConsumerBuilder<T>
    {
        return builder.WhenUndeclaredMessageTypeArrives(x =>
        {
            x.Log = true;
            x.Fail = false;
        });
    }
}
