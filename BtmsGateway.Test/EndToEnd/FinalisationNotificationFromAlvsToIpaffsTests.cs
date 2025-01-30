using System.Net;
using System.Net.Mime;
using System.Text;
using BtmsGateway.Test.TestUtils;
using FluentAssertions;

namespace BtmsGateway.Test.EndToEnd;

public class FinalisationNotificationFromAlvsToIpaffsTests : TargetRoutingTestBase
{
    private const string OriginalPath = "/finalisation-notification/path";
    private const string GatewayPath = $"/alvs_ipaffs{OriginalPath}";
    
    private readonly string _originalRequestSoap = File.ReadAllText(Path.Combine(FixturesPath, "AlvsToIpaffsFinalisationNotification.xml"));
    private readonly string _btmsRequestJson = File.ReadAllText(Path.Combine(FixturesPath, "FinalisationNotification.json")).LinuxLineEndings();
    private readonly StringContent _originalRequestSoapContent;

    public FinalisationNotificationFromAlvsToIpaffsTests()
    {
        _originalRequestSoapContent = new StringContent(_originalRequestSoap, Encoding.UTF8, MediaTypeNames.Application.Soap);
        TestWebServer.RoutedHttpHandler.SetNextResponse(statusFunc: () => HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task When_receiving_request_from_alvs_Then_should_forward_to_cds()
    {
        await HttpClient.PostAsync(GatewayPath, _originalRequestSoapContent);

        TestWebServer.RoutedHttpHandler.LastRequest!.RequestUri!.AbsoluteUri.Should().Be($"http://alvs_ipaffs{OriginalPath}");
        (await TestWebServer.RoutedHttpHandler.LastRequest!.Content!.ReadAsStringAsync()).Should().Be(_originalRequestSoap);
    }

    [Fact]
    public async Task When_receiving_request_from_alvs_Then_should_respond_with_cds_response()
    {
        var response = await HttpClient.PostAsync(GatewayPath, _originalRequestSoapContent);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await response.Content.ReadAsStringAsync()).Should().Be(string.Empty);
    }

    [Fact]
    public async Task When_receiving_request_from_alvs_Then_should_forward_converted_json_to_btms()
    {
        await HttpClient.PostAsync(GatewayPath, _originalRequestSoapContent);

        TestWebServer.ForkedHttpHandler.LastRequest!.RequestUri!.AbsoluteUri.Should().Be($"http://btms{OriginalPath}");
        (await TestWebServer.ForkedHttpHandler.LastRequest!.Content!.ReadAsStringAsync()).LinuxLineEndings().Should().Be(_btmsRequestJson);
    }
}