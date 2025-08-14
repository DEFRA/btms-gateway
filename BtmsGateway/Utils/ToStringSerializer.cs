using System.Diagnostics.CodeAnalysis;
using System.Text;
using SlimMessageBus.Host.Serialization;

namespace BtmsGateway.Utils;

public class ToStringSerializer : IMessageSerializer, IMessageSerializer<string>, IMessageSerializerProvider
{
    [ExcludeFromCodeCoverage]
    public byte[] Serialize(
        Type messageType,
        IDictionary<string, object> headers,
        object message,
        object transportMessage
    )
    {
        return Encoding.UTF8.GetBytes(message.ToString()!);
    }

    public object Deserialize(
        Type messageType,
        IReadOnlyDictionary<string, object> headers,
        string payload,
        object transportMessage
    )
    {
        return payload;
    }

    public object Deserialize(
        Type messageType,
        IReadOnlyDictionary<string, object> headers,
        byte[] payload,
        object transportMessage
    )
    {
        return Encoding.UTF8.GetString(payload);
    }

    [ExcludeFromCodeCoverage]
    string IMessageSerializer<string>.Serialize(
        Type messageType,
        IDictionary<string, object> headers,
        object message,
        object transportMessage
    )
    {
        return message.ToString()!;
    }

    [ExcludeFromCodeCoverage]
    public IMessageSerializer GetSerializer(string path) => this;
}
