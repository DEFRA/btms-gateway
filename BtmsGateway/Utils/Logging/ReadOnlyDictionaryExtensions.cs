namespace BtmsGateway.Utils.Logging;

public static class ReadOnlyDictionaryExtensions
{
    public static string? GetTraceId(this IReadOnlyDictionary<string, object> headers, string traceHeader)
    {
        return headers.TryGetValue(traceHeader, out var traceId) ? traceId.ToString()?.Replace("-", "") : null;
    }
}
