using System.Net;
using System.Net.Mime;
using System.Text;
using BtmsGateway.Test.TestUtils;
using FluentAssertions;

namespace BtmsGateway.Test.EndToEnd;

public class ClearanceRequestFromAlvsToIpaffsTests : TargetRoutingTestBase
{
    private const string UrlPath = "/route/path/alvs-ipaffs/clearance-request";

    private readonly string _alvsRequestSoap = File.ReadAllText(
        Path.Combine(FixturesPath, "AlvsToIpaffsClearanceRequest.xml")
    );
    private readonly string _alvsResponseSoap = File.ReadAllText(Path.Combine(FixturesPath, "IpaffsResponse.xml"));
    private readonly string _btmsRequestJson = File.ReadAllText(Path.Combine(FixturesPath, "ClearanceRequest.json"))
        .LinuxLineEndings();
    private readonly StringContent _alvsRequestSoapContent;

    public ClearanceRequestFromAlvsToIpaffsTests()
    {
        _alvsRequestSoapContent = new StringContent(_alvsRequestSoap, Encoding.UTF8, MediaTypeNames.Text.Xml);
        TestWebServer.RoutedHttpHandler.SetNextResponse(
            content: _alvsResponseSoap,
            statusFunc: () => HttpStatusCode.Accepted
        );
    }

    [Fact]
    public async Task When_receiving_request_from_alvs_Then_should_forward_to_alvs()
    {
        await HttpClient.PostAsync(UrlPath, _alvsRequestSoapContent);

        TestWebServer
            .RoutedHttpHandler.LastRequest!.RequestUri!.AbsoluteUri.Should()
            .Be($"http://alvs-ipaffs-host{UrlPath}");
        (await TestWebServer.RoutedHttpHandler.LastRequest!.Content!.ReadAsStringAsync()).Should().Be(_alvsRequestSoap);
    }

    [Fact]
    public async Task When_receiving_request_from_alvs_Then_should_respond_with_ipaffs_response()
    {
        var response = await HttpClient.PostAsync(UrlPath, _alvsRequestSoapContent);

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
        (await response.Content.ReadAsStringAsync()).Should().Be(_alvsResponseSoap);
    }

    [Fact]
    public async Task When_receiving_request_from_alvs_Then_should_forward_converted_json_to_btms()
    {
        await HttpClient.PostAsync(UrlPath, _alvsRequestSoapContent);

        TestWebServer.ForkedHttpHandler.LastRequest!.RequestUri!.AbsoluteUri.Should().Be($"http://btms-host{UrlPath}");
        (await TestWebServer.ForkedHttpHandler.LastRequest!.Content!.ReadAsStringAsync())
            .LinuxLineEndings()
            .Should()
            .Be(_btmsRequestJson);
    }
}
