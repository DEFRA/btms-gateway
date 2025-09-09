using System.Net;
using BtmsGateway.IntegrationTests.TestBase;
using BtmsGateway.IntegrationTests.TestUtils;
using BtmsGateway.Utils.Http;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using WireMock.Client;
using WireMock.Client.Extensions;

namespace BtmsGateway.IntegrationTests.Utils.Http;

[Collection("UsesWireMockClient")]
public class ProxyTests(WireMockClient wireMockClient) : IntegrationTestBase
{
    private readonly IWireMockAdminApi _wireMockAdminApi = wireMockClient.WireMockAdminApi;

    [Fact]
    public async Task When_post_to_cds_takes_longer_than_http_client_timeout_Then_service_unavailable_returned()
    {
        var fixtureContent = FixtureTest.UsingContent("DecisionNotification.xml");

        await using var application = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("IntegrationTests");
        });

        var configuration = application.Services.GetService(typeof(IConfiguration));
        var httpClientTimeoutSeconds = ((ConfigurationManager)configuration!).GetValue<int>("HttpClientTimeoutSeconds");

        var responseDelay =
            httpClientTimeoutSeconds > 0 ? httpClientTimeoutSeconds + 1 : Proxy.DefaultHttpClientTimeoutSeconds + 1;

        var postMappingBuilder = _wireMockAdminApi.GetMappingBuilder();
        postMappingBuilder.Given(m =>
            m.WithRequest(req => req.UsingPost().WithPath("/cds/ws/CDS/defra/alvsclearanceinbound/v1"))
                .WithResponse(rsp =>
                    rsp.WithStatusCode(HttpStatusCode.ServiceUnavailable).WithDelay(TimeSpan.FromSeconds(responseDelay))
                )
        );
        var postStatus = await postMappingBuilder.BuildAndPostAsync();
        Assert.NotNull(postStatus.Guid);

        var putMappingBuilder = _wireMockAdminApi.GetMappingBuilder();
        putMappingBuilder.Given(m =>
            m.WithRequest(req => req.UsingPut().WithPath("/comparer/alvs-decisions/25GB1HG99NHUJO3999"))
                .WithResponse(rsp =>
                    rsp.WithStatusCode(HttpStatusCode.ServiceUnavailable).WithDelay(TimeSpan.FromSeconds(responseDelay))
                )
        );
        var putStatus = await putMappingBuilder.BuildAndPostAsync();
        Assert.NotNull(putStatus.Guid);

        using var client = application.CreateClient();

        var response = await client.PostAsync(
            "/ws/CDS/defra/alvsclearanceinbound/v1",
            new StringContent(fixtureContent)
        );

        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public async Task When_post_to_ipaffs_takes_longer_than_http_client_timeout_Then_service_unavailable_returned()
    {
        var fixtureContent = FixtureTest.UsingContent("SearchCertificate.xml");

        await using var application = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("IntegrationTests");
        });

        var configuration = application.Services.GetService(typeof(IConfiguration));
        var httpClientTimeoutSeconds = ((ConfigurationManager)configuration!).GetValue<int>(
            "AlvsIpaffsHttpClientTimeoutSeconds"
        );

        var responseDelay =
            httpClientTimeoutSeconds > 0
                ? httpClientTimeoutSeconds + 1
                : Proxy.DefaultAlvsIpaffsHttpClientTimeoutSeconds + 1;

        var postMappingBuilder = _wireMockAdminApi.GetMappingBuilder();
        postMappingBuilder.Given(m =>
            m.WithRequest(req => req.UsingPost().WithPath("/ipaffs/soapsearch/tst/sanco/traces_ws/searchCertificate"))
                .WithResponse(rsp =>
                    rsp.WithStatusCode(HttpStatusCode.ServiceUnavailable).WithDelay(TimeSpan.FromSeconds(responseDelay))
                )
        );
        var postStatus = await postMappingBuilder.BuildAndPostAsync();
        Assert.NotNull(postStatus.Guid);

        using var client = application.CreateClient();

        var response = await client.PostAsync(
            "/soapsearch/tst/sanco/traces_ws/searchCertificate",
            new StringContent(fixtureContent)
        );

        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    }
}
