using BtmsGateway.Utils;
using FluentAssertions;

namespace BtmsGateway.Test.Utils;

public class ToStringSerializerTests
{
    private readonly ToStringSerializer _toStringSerializer = new();

    [Fact]
    public void Deserialize_String_Returns_String()
    {
        _toStringSerializer
            .Deserialize(typeof(string), new Dictionary<string, object>(), "sosig", null!)
            .Should()
            .Be("sosig");
    }

    [Fact]
    public void Deserialize_Byte_Returns_String()
    {
        _toStringSerializer
            .Deserialize(typeof(string), new Dictionary<string, object>(), "sosig"u8.ToArray(), null!)
            .Should()
            .Be("sosig");
    }
}
