using System.IO.Compression;
using System.Text;
using BtmsGateway.Utils;
using FluentAssertions;

namespace BtmsGateway.Test.Utils;

public class MessageDeserializerTests
{
    private const string Content = """{"hello": "there"}""";

    [Fact]
    public void Deserialize_WhenGivenACompressedMessage_ThenShouldReturnDecompressedMessage()
    {
        var result = MessageDeserializer.Deserialize<object>(CompressMessage(Content), "gzip, base64")!;

        result.ToString().Should().Be(Content);
    }

    [Fact]
    public void Deserialize_WhenGivenAnUncompressedMessage_ThenShouldReturnUncompressedMessage()
    {
        var result = MessageDeserializer.Deserialize<object>(Content, null)!;

        result.ToString().Should().Be(Content);
    }

    [Fact]
    public void Deserialize_WhenGivenInvalidContentType_ShouldThrowException()
    {
        Assert.Throws<NotImplementedException>(() => MessageDeserializer.Deserialize<object>(Content, "brotli"));
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
