using System.Diagnostics.CodeAnalysis;
using System.Text;
using SlimMessageBus.Host.Serialization;

namespace BtmsGateway.Utils;

public class ToStringSerializer : IMessageSerializer, IMessageSerializer<string>, IMessageSerializerProvider
{
    [ExcludeFromCodeCoverage]
    public byte[] Serialize(Type t, object message)
    {
        throw new NotImplementedException();
    }

    public object Deserialize(Type t, string payload)
    {
        return payload;
    }

    public object Deserialize(Type t, byte[] payload)
    {
        return Encoding.UTF8.GetString(payload);
    }

    [ExcludeFromCodeCoverage]
    string IMessageSerializer<string>.Serialize(Type t, object message)
    {
        return message.ToString()!;
    }

    [ExcludeFromCodeCoverage]
    public IMessageSerializer GetSerializer(string path) => this;
}
