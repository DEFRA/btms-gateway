using System.Net;
using BtmsGateway.IntegrationTests.TestUtils;
using BtmsGateway.Utils.Http;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using WireMock.Client;
using WireMock.Client.Extensions;

namespace BtmsGateway.IntegrationTests.Utils.Http;

[Collection("UsesWireMockClient")]
public class ProxyTests(WireMockClient wireMockClient)
{
    private readonly IWireMockAdminApi _wireMockAdminApi = wireMockClient.WireMockAdminApi;

    [Fact]
    public async Task When_post_to_cds_takes_longer_than_http_client_timeout_Then_service_unavailable_returned()
    {
        var fixtureContent = FixtureTest.UsingContent("DecisionNotification.xml");

        await using var application = new WebApplicationFactory<Program>();

        var configuration = application.Services.GetService(typeof(IConfiguration));
        var httpClientTimeoutSeconds = ((ConfigurationManager)configuration!).GetValue<int>("HttpClientTimeoutSeconds");

        var responseDelay =
            httpClientTimeoutSeconds > 0 ? httpClientTimeoutSeconds + 1 : Proxy.DefaultHttpClientTimeoutSeconds + 1;

        var mappingBuilder = _wireMockAdminApi.GetMappingBuilder();
        mappingBuilder.Given(m =>
            m.WithRequest(req => req.UsingPost().WithPath("/ws/CDS/defra/alvsclearanceinbound/v1"))
                .WithResponse(rsp => rsp.WithStatusCode(HttpStatusCode.ServiceUnavailable).WithDelay(responseDelay))
        );

        using var client = application.CreateClient();

        var response = await client.PostAsync(
            "/ws/CDS/defra/alvsclearanceinbound/v1",
            new StringContent(fixtureContent)
        );

        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    }
}
