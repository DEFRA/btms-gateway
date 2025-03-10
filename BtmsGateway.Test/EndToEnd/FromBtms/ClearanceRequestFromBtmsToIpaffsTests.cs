using System.Net;
using System.Net.Mime;
using System.Text;
using BtmsGateway.Test.TestUtils;
using FluentAssertions;

namespace BtmsGateway.Test.EndToEnd.FromBtms;

public class ClearanceRequestFromBtmsToIpaffsTests : TargetRoutingTestBase
{
    private const string OriginalPath = "/clearance-request/path";
    private const string GatewayPath = $"/alvs_ipaffs{OriginalPath}";

    private readonly string _btmsRequestJson = File.ReadAllText(Path.Combine(FixturesPath, "ClearanceRequest.json")).LinuxLineEndings();
    private readonly string _btmsResponseJson = File.ReadAllText(Path.Combine(FixturesPath, "IpaffsResponse.json"));
    private readonly string _ipaffsRequestSoap = File.ReadAllText(Path.Combine(FixturesPath, "AlvsToIpaffsClearanceRequest.xml"));
    private readonly StringContent _btmsRequestJsonContent;

    public ClearanceRequestFromBtmsToIpaffsTests()
    {
        _btmsRequestJsonContent = new StringContent(_btmsRequestJson, Encoding.UTF8, MediaTypeNames.Application.Json);
        TestWebServer.RoutedHttpHandler.SetNextResponse(content: _btmsResponseJson, statusFunc: () => HttpStatusCode.Accepted);
    }

    [Fact(Skip = "Not implemented outbound from BTMS yet")]
    public async Task When_receiving_request_from_btms_Then_should_forward_converted_soap_to_alvs()
    {
        await HttpClient.PostAsync(GatewayPath, _btmsRequestJsonContent);

        TestWebServer.RoutedHttpHandler.LastRequest!.RequestUri!.AbsoluteUri.Should().Be($"http://alvs_ipaffs{OriginalPath}");
        (await TestWebServer.RoutedHttpHandler.LastRequest!.Content!.ReadAsStringAsync()).Should().Be(_ipaffsRequestSoap);
    }

    [Fact(Skip = "Not implemented outbound from BTMS yet")]
    public async Task When_receiving_request_from_btms_Then_should_respond_with_ipaffs_response()
    {
        var response = await HttpClient.PostAsync(GatewayPath, _btmsRequestJsonContent);

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
        (await response.Content.ReadAsStringAsync()).Should().Be(_btmsResponseJson);
    }

    [Fact(Skip = "Not implemented outbound from BTMS yet")]
    public async Task When_receiving_request_from_btms_Then_should_not_forward_btms()
    {
        await HttpClient.PostAsync(GatewayPath, _btmsRequestJsonContent);

        TestWebServer.ForkedHttpHandler.LastRequest.Should().BeNull();
    }
}