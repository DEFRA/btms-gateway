using BtmsGateway.Utils.Logging;
using FluentAssertions;

namespace BtmsGateway.Test.Utils.Logging;

public class ReadOnlyDictionaryExtensionsTests
{
    [Fact]
    public void When_getting_trace_id_and_header_exists_Then_should_return_trace_id()
    {
        IReadOnlyDictionary<string, object> headers = new Dictionary<string, object>
        {
            { "trace-id-header", "trace-id-header-value" }
        };

        var result = headers.GetTraceId("trace-id-header");

        result.Should().Be("traceidheadervalue");
    }

    [Fact]
    public void When_getting_trace_id_and_header_does_not_exist_Then_should_return_empty()
    {
        IReadOnlyDictionary<string, object> headers = new Dictionary<string, object>();

        var result = headers.GetTraceId("trace-id-header");

        result.Should().BeNull();
    }
}