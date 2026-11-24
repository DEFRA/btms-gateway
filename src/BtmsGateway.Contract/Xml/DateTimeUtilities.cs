using System.Globalization;

namespace BtmsGateway.Contract.Xml;

internal static class DateTimeUtilities
{
    private const string CompactXmlDateTimeFormat = "yyyyMMddHHmm";
    private const string IsoDateTimeFormat = "yyyy-MM-ddTHH:mm:ss.fff";

    public static DateTime? ParseNullableXmlDateTime(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || string.Equals(value, "null", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (
            DateTime.TryParseExact(
                value,
                CompactXmlDateTimeFormat,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var compactDateTime
            )
        )
        {
            return compactDateTime;
        }

        return DateTime.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
    }

    public static string? ToCompactXmlDateTime(DateTime? value)
    {
        return value?.ToString(CompactXmlDateTimeFormat, CultureInfo.InvariantCulture);
    }

    public static string? ToIsoDateTime(DateTime? value)
    {
        return value?.ToString(IsoDateTimeFormat);
    }
}
