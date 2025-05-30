using System.Net;
using System.Net.Mime;
using System.Text;
using FluentAssertions;

namespace BtmsGateway.Test.EndToEnd;

public class FinalisationNotificationFromAlvsToIpaffsTests : TargetRoutingTestBase
{
    private const string UrlPath = "/route/path/alvs-ipaffs/finalisation-notification";

    private readonly string _alvsRequestSoap = File.ReadAllText(
        Path.Combine(FixturesPath, "AlvsToIpaffsFinalisationNotification.xml")
    );
    private readonly string _alvsResponseSoap = File.ReadAllText(Path.Combine(FixturesPath, "IpaffsResponse.xml"));
    private readonly StringContent _alvsRequestSoapContent;

    public FinalisationNotificationFromAlvsToIpaffsTests()
    {
        _alvsRequestSoapContent = new StringContent(_alvsRequestSoap, Encoding.UTF8, MediaTypeNames.Text.Xml);
        TestWebServer.RoutedHttpHandler.SetNextResponse(
            content: _alvsResponseSoap,
            statusFunc: () => HttpStatusCode.Accepted
        );
    }

    [Fact]
    public async Task When_receiving_request_from_alvs_Then_should_forward_to_ipaffs()
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
}
