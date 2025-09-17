using System.Net;
using System.Net.Http.Headers;
using BtmsGateway.IntegrationTests.TestBase;
using BtmsGateway.IntegrationTests.TestUtils;
using FluentAssertions;
using WireMock.Client;
using WireMock.Client.Extensions;

namespace BtmsGateway.IntegrationTests.EndToEnd.Ipaffs;

[Collection("UsesWireMockClient")]
public class AlvsToIpaffsTests(WireMockClient wireMockClient) : IntegrationTestBase
{
    private readonly IWireMockAdminApi _wireMockAdminApi = wireMockClient.WireMockAdminApi;
    private readonly string _ipaffsClearanceRequest = FixtureTest.UsingContent("AlvsToIpaffsClearanceRequest.xml");
    private readonly string _ipaffsFinalisation = FixtureTest.UsingContent("AlvsToIpaffsFinalisation.xml");
    private readonly string _ipaffsDecision = FixtureTest.UsingContent("AlvsToIpaffsDecision.xml");
    private readonly string _ipaffsSearchCertificate = FixtureTest.UsingContent("AlvsToIpaffsSearchCertificate.xml");
    private readonly string _ipaffsPollSearchCertificate = FixtureTest.UsingContent(
        "AlvsToIpaffsPollSearchCertificate.xml"
    );

    [Fact]
    public async Task When_receiving_ipaffs_clearance_request_from_alvs_Then_should_respond_to_alvs_with_ok_and_canned_success_response()
    {
        var httpClient = CreateHttpClient(false);
        var response = await httpClient.PostAsync(
            Testing.Endpoints.Ipaffs.PostClearanceRequest(),
            new StringContent(_ipaffsClearanceRequest, new MediaTypeHeaderValue("application/xml"))
        );

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType.Should().Be(new MediaTypeHeaderValue("application/xml"));
        var verifyResponseSettings = new VerifySettings();
        await VerifyXml(await response.Content.ReadAsStringAsync(), verifyResponseSettings);
    }

    [Fact]
    public async Task When_receiving_ipaffs_finalisation_from_alvs_Then_should_respond_to_alvs_with_ok_and_canned_success_response()
    {
        var httpClient = CreateHttpClient(false);
        var response = await httpClient.PostAsync(
            Testing.Endpoints.Ipaffs.PostFinalisation(),
            new StringContent(_ipaffsFinalisation, new MediaTypeHeaderValue("application/xml"))
        );

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType.Should().Be(new MediaTypeHeaderValue("application/xml"));
        var verifyResponseSettings = new VerifySettings();
        await VerifyXml(await response.Content.ReadAsStringAsync(), verifyResponseSettings);
    }

    [Fact]
    public async Task When_receiving_ipaffs_decision_from_alvs_Then_should_respond_to_alvs_with_ok_and_canned_success_response()
    {
        var httpClient = CreateHttpClient(false);
        var response = await httpClient.PostAsync(
            Testing.Endpoints.Ipaffs.PostDecision(),
            new StringContent(_ipaffsDecision, new MediaTypeHeaderValue("application/xml"))
        );

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType.Should().Be(new MediaTypeHeaderValue("application/xml"));
        var verifyResponseSettings = new VerifySettings();
        await VerifyXml(await response.Content.ReadAsStringAsync(), verifyResponseSettings);
    }

    [Theory]
    [InlineData(HttpStatusCode.OK, "<ipaffs>Some IPAFFS Response</ipaffs>")]
    [InlineData(HttpStatusCode.Accepted, "<ipaffs>Some IPAFFS Response</ipaffs>")]
    [InlineData(HttpStatusCode.NoContent, "")]
    [InlineData(HttpStatusCode.NotFound, "<ipaffs>Some IPAFFS Response</ipaffs>")]
    [InlineData(HttpStatusCode.BadRequest, "<ipaffs>Some IPAFFS Response</ipaffs>")]
    public async Task When_receiving_ipaffs_search_certificate_from_alvs_Then_should_forward_to_ipaffs_and_respond_with_ipaffs_response(
        HttpStatusCode statusCode,
        string ipaffsResponseBody
    )
    {
        var postMappingBuilder = _wireMockAdminApi.GetMappingBuilder();
        postMappingBuilder.Given(m =>
            m.WithRequest(req => req.UsingPost().WithPath($"/ipaffs{Testing.Endpoints.Ipaffs.PostSearchCertificate()}"))
                .WithResponse(rsp => rsp.WithStatusCode(statusCode).WithBody(ipaffsResponseBody))
        );
        var postStatus = await postMappingBuilder.BuildAndPostAsync();
        Assert.NotNull(postStatus.Guid);

        var httpClient = CreateHttpClient(false);
        var response = await httpClient.PostAsync(
            Testing.Endpoints.Ipaffs.PostSearchCertificate(),
            new StringContent(_ipaffsSearchCertificate, new MediaTypeHeaderValue("application/xml"))
        );

        response.StatusCode.Should().Be(statusCode);
        response.Content.Headers.ContentType.Should().Be(new MediaTypeHeaderValue("application/xml"));
        var responseBody = await response.Content.ReadAsStringAsync();
        responseBody.Should().Be(ipaffsResponseBody);
    }

    [Theory]
    [InlineData(HttpStatusCode.OK, "<ipaffs>Some IPAFFS Response</ipaffs>")]
    [InlineData(HttpStatusCode.Accepted, "<ipaffs>Some IPAFFS Response</ipaffs>")]
    [InlineData(HttpStatusCode.NoContent, "")]
    [InlineData(HttpStatusCode.NotFound, "<ipaffs>Some IPAFFS Response</ipaffs>")]
    [InlineData(HttpStatusCode.BadRequest, "<ipaffs>Some IPAFFS Response</ipaffs>")]
    public async Task When_receiving_ipaffs_poll_search_certificate_from_alvs_Then_should_forward_to_ipaffs_and_respond_with_ipaffs_response(
        HttpStatusCode statusCode,
        string ipaffsResponseBody
    )
    {
        var postMappingBuilder = _wireMockAdminApi.GetMappingBuilder();
        postMappingBuilder.Given(m =>
            m.WithRequest(req =>
                    req.UsingPost().WithPath($"/ipaffs{Testing.Endpoints.Ipaffs.PostPollSearchCertificate()}")
                )
                .WithResponse(rsp => rsp.WithStatusCode(statusCode).WithBody(ipaffsResponseBody))
        );
        var postStatus = await postMappingBuilder.BuildAndPostAsync();
        Assert.NotNull(postStatus.Guid);

        var httpClient = CreateHttpClient(false);
        var response = await httpClient.PostAsync(
            Testing.Endpoints.Ipaffs.PostPollSearchCertificate(),
            new StringContent(_ipaffsPollSearchCertificate, new MediaTypeHeaderValue("application/xml"))
        );

        response.StatusCode.Should().Be(statusCode);
        response.Content.Headers.ContentType.Should().Be(new MediaTypeHeaderValue("application/xml"));
        var responseBody = await response.Content.ReadAsStringAsync();
        responseBody.Should().Be(ipaffsResponseBody);
    }
}
