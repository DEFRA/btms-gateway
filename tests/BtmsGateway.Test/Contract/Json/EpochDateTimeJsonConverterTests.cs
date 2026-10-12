using System.Text.Json;
using BtmsGateway.Contract.Json;
using FluentAssertions;

namespace BtmsGateway.Test.Contract.Json;

public class EpochDateTimeJsonConverterTests
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        Converters = { new EpochDateTimeJsonConverter() },
    };

    [Fact]
    public void When_deserializing_unix_seconds_Then_it_should_parse_seconds_from_epoch()
    {
        var result = JsonSerializer.Deserialize<DateTime>("1723127967", SerializerOptions);

        result.Should().Be(DateTime.UnixEpoch.AddSeconds(1723127967));
    }

    [Fact]
    public void When_deserializing_unix_milliseconds_Then_it_should_parse_milliseconds_from_epoch()
    {
        var result = JsonSerializer.Deserialize<DateTime>("1712851200000", SerializerOptions);

        result.Should().Be(DateTime.UnixEpoch.AddMilliseconds(1712851200000));
    }

    [Fact]
    public void When_deserializing_iso_datetime_string_Then_it_should_parse_using_roundtrip_kind()
    {
        var result = JsonSerializer.Deserialize<DateTime>("\"2025-05-29T18:57:29.298Z\"", SerializerOptions);

        result.Should().Be(DateTime.Parse("2025-05-29T18:57:29.298Z", null, System.Globalization.DateTimeStyles.RoundtripKind));
    }

    [Fact]
    public void When_serializing_datetime_Then_it_should_write_unix_milliseconds()
    {
        var value = DateTime.UnixEpoch.AddMilliseconds(1712851200000);

        var result = JsonSerializer.Serialize(value, SerializerOptions);

        result.Should().Be("1712851200000");
    }
}
