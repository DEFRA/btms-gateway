using System.Net;
using FluentAssertions;

namespace BtmsGateway.Test.EndToEnd;

public class PassThroughRequestTests : TargetRoutingTestBase
{
    [Fact]
    public async Task When_receiving_unrouted_request_Then_should_return_200()
    {
        var result = await HttpClient.GetAsync("/health");

        result.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
