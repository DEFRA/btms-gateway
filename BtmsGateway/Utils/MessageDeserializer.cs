using System.IO.Compression;
using System.Text;
using System.Text.Json;

namespace BtmsGateway.Utils;

public static class MessageDeserializer
{
    private static readonly JsonSerializerOptions s_jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public static T? Deserialize<T>(string message, string? contentEncoding)
    {
        if (contentEncoding != null && contentEncoding != "gzip, base64")
        {
            throw new NotImplementedException(
                "Only 'gzip, base64' content encoding is supported, passed: " + contentEncoding
            );
        }

        if (contentEncoding == null)
            return JsonSerializer.Deserialize<T>(message, s_jsonOptions);

        var compressedBytes = Convert.FromBase64String(message);
        using var compressedStream = new MemoryStream(compressedBytes);
        using var gzipStream = new GZipStream(compressedStream, CompressionMode.Decompress);
        using var reader = new StreamReader(gzipStream, Encoding.UTF8);

        return JsonSerializer.Deserialize<T>(reader.ReadToEnd(), s_jsonOptions);
    }
}
