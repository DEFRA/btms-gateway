using System.Net;
using BtmsGateway.IntegrationTests.TestBase;
using FluentAssertions;

namespace BtmsGateway.IntegrationTests.EndToEnd.PassThrough;

public class PassThroughTests : IntegrationTestBase
{
    [Fact]
    public async Task When_receiving_request_that_is_passthrough_route_Then_should_return_response()
    {
        var httpClient = CreateHttpClient(false);
        var response = await httpClient.GetAsync(Testing.Endpoints.Passthrough.GetHealth());

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
