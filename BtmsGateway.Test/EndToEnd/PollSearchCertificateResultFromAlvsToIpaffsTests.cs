using System.Net;
using System.Net.Mime;
using System.Text;
using BtmsGateway.Test.TestUtils;
using FluentAssertions;

namespace BtmsGateway.Test.EndToEnd;

public class PollSearchCertificateResultFromAlvsToIpaffsTests : TargetRoutingTestBase
{
    private const string UrlPath = "/soapsearch/tst/sanco/traces_ws/pollSearchCertificateResult";

    private readonly string _alvsRequestSoap = File.ReadAllText(Path.Combine(FixturesPath, "AlvsToIpaffsPollSearchCertificateResult.xml"));
    private readonly string _ipaffsResponseSoap = File.ReadAllText(Path.Combine(FixturesPath, "IpaffsResponse.xml"));
    private readonly StringContent _alvsRequestSoapContent;

    public PollSearchCertificateResultFromAlvsToIpaffsTests()
    {
        _alvsRequestSoapContent = new StringContent(_alvsRequestSoap, Encoding.UTF8, MediaTypeNames.Application.Soap);
        TestWebServer.RoutedHttpHandler.SetNextResponse(content: _ipaffsResponseSoap, statusFunc: () => HttpStatusCode.Accepted);
    }

    [Fact]
    public async Task When_receiving_poll_search_certificate_result_from_alvs_Then_should_forward_to_ipaffs()
    {
        await HttpClient.PostAsync(UrlPath, _alvsRequestSoapContent);

        TestWebServer.RoutedHttpHandler.LastRequest!.RequestUri!.AbsoluteUri.Should().Be($"http://alvs-ipaffs-host{UrlPath}");
        (await TestWebServer.RoutedHttpHandler.LastRequest!.Content!.ReadAsStringAsync()).Should().Be(_alvsRequestSoap);
    }

    [Fact]
    public async Task When_receiving_poll_search_certificate_result_from_alvs_Then_should_respond_with_ipaffs_response()
    {
        var response = await HttpClient.PostAsync(UrlPath, _alvsRequestSoapContent);

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
        (await response.Content.ReadAsStringAsync()).Should().Be(_ipaffsResponseSoap);
    }
}