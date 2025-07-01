using BtmsGateway.Utils;
using FluentAssertions;

namespace BtmsGateway.Test.Utils;

public class ToStringSerializerTests
{
    private readonly ToStringSerializer _toStringSerializer = new();

    [Fact]
    public void Deserialize_String_Returns_String()
    {
        _toStringSerializer.Deserialize(null!, "sosig").Should().Be("sosig");
    }

    [Fact]
    public void Deserialize_Byte_Returns_String()
    {
        _toStringSerializer.Deserialize(null!, "sosig"u8.ToArray()).Should().Be("sosig");
    }
}
