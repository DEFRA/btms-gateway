using System.Net;
using System.Net.Mime;
using System.Text;
using BtmsGateway.Test.TestUtils;
using FluentAssertions;

namespace BtmsGateway.Test.EndToEnd.FromBtms;

public class FinalisationNotificationFromBtmsToIpaffsTests : TargetRoutingTestBase
{
    private const string UrlPath = "/route/path/alvs-ipaffs/finalisation-notification";

    private readonly string _btmsRequestJson = File.ReadAllText(Path.Combine(FixturesPath, "FinalisationNotification.json")).LinuxLineEndings();
    private readonly string _btmsResponseJson = File.ReadAllText(Path.Combine(FixturesPath, "IpaffsResponse.json"));
    private readonly string _ipaffsRequestSoap = File.ReadAllText(Path.Combine(FixturesPath, "AlvsToIpaffsFinalisationNotification.xml"));
    private readonly StringContent _btmsRequestJsonContent;

    public FinalisationNotificationFromBtmsToIpaffsTests()
    {
        _btmsRequestJsonContent = new StringContent(_btmsRequestJson, Encoding.UTF8, MediaTypeNames.Application.Json);
        TestWebServer.RoutedHttpHandler.SetNextResponse(content: _btmsRequestJson, statusFunc: () => HttpStatusCode.Accepted);
    }

    [Fact(Skip = "Not implemented outbound from BTMS yet")]
    public async Task When_receiving_request_from_btms_Then_should_forward_converted_soap_to_ipaffs()
    {
        await HttpClient.PostAsync(UrlPath, _btmsRequestJsonContent);

        TestWebServer.RoutedHttpHandler.LastRequest!.RequestUri!.AbsoluteUri.Should().Be($"http://alvs-ipaffs-host{UrlPath}");
        (await TestWebServer.RoutedHttpHandler.LastRequest!.Content!.ReadAsStringAsync()).Should().Be(_ipaffsRequestSoap);
    }

    [Fact(Skip = "Not implemented outbound from BTMS yet")]
    public async Task When_receiving_request_from_btms_Then_should_respond_with_ipaffs_response()
    {
        var response = await HttpClient.PostAsync(UrlPath, _btmsRequestJsonContent);

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
        (await response.Content.ReadAsStringAsync()).Should().Be(_btmsResponseJson);
    }

    [Fact(Skip = "Not implemented outbound from BTMS yet")]
    public async Task When_receiving_request_from_btms_Then_should_not_forward()
    {
        await HttpClient.PostAsync(UrlPath, _btmsRequestJsonContent);

        TestWebServer.ForkedHttpHandler.LastRequest.Should().BeNull();
    }
}