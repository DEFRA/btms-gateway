using System.Net;
using System.Net.Mime;
using System.Text;
using BtmsGateway.Test.TestUtils;
using FluentAssertions;

namespace BtmsGateway.Test.EndToEnd;

public class ClearanceRequestFromAlvsToIpaffsTests : TargetRoutingTestBase
{
    private const string UrlPath = "/route/path/alvs-ipaffs/clearance-request";
    
    private readonly string _originalRequestSoap = File.ReadAllText(Path.Combine(FixturesPath, "AlvsToIpaffsClearanceRequest.xml"));
    private readonly string _originalResponseSoap = File.ReadAllText(Path.Combine(FixturesPath, "AlvsResponse.xml"));
    private readonly string _btmsRequestJson = File.ReadAllText(Path.Combine(FixturesPath, "ClearanceRequest.json")).LinuxLineEndings();
    private readonly StringContent _originalRequestSoapContent;

    public ClearanceRequestFromAlvsToIpaffsTests()
    {
        _originalRequestSoapContent = new StringContent(_originalRequestSoap, Encoding.UTF8, MediaTypeNames.Text.Xml);
        TestWebServer.RoutedHttpHandler.SetNextResponse(content: _originalResponseSoap, statusFunc: () => HttpStatusCode.Accepted);
    }

    [Fact]
    public async Task When_receiving_request_from_cds_Then_should_forward_to_alvs()
    {
        await HttpClient.PostAsync(UrlPath, _originalRequestSoapContent);

        TestWebServer.RoutedHttpHandler.LastRequest!.RequestUri!.AbsoluteUri.Should().Be($"http://alvs-ipaffs-host{UrlPath}");
        (await TestWebServer.RoutedHttpHandler.LastRequest!.Content!.ReadAsStringAsync()).Should().Be(_originalRequestSoap);
    }

    [Fact]
    public async Task When_receiving_request_from_cds_Then_should_respond_with_alvs_response()
    {
        var response = await HttpClient.PostAsync(UrlPath, _originalRequestSoapContent);

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
        (await response.Content.ReadAsStringAsync()).Should().Be(_originalResponseSoap);
    }

    [Fact]
    public async Task When_receiving_request_from_cds_Then_should_forward_converted_json_to_btms()
    {
        await HttpClient.PostAsync(UrlPath, _originalRequestSoapContent);

        TestWebServer.ForkedHttpHandler.LastRequest!.RequestUri!.AbsoluteUri.Should().Be($"http://btms-host{UrlPath}");
        (await TestWebServer.ForkedHttpHandler.LastRequest!.Content!.ReadAsStringAsync()).LinuxLineEndings().Should().Be(_btmsRequestJson);
    }
}