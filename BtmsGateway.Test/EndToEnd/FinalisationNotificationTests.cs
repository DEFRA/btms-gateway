using System.Net;
using System.Net.Mime;
using System.Text;
using BtmsGateway.Test.TestUtils;
using FluentAssertions;

namespace BtmsGateway.Test.EndToEnd;

public class FinalisationNotificationTests : TargetRoutingTestBase
{
    private const string OriginalPath = "/finalisation-notification/path";
    private const string GatewayPath = $"/cds{OriginalPath}";
    private const string BtmsPath = $"/forked{OriginalPath}";
    
    private readonly string _originalRequestSoap = File.ReadAllText(Path.Combine(FixturesPath, "FinalisationNotification.xml"));
    private readonly string _originalResponseSoap = File.ReadAllText(Path.Combine(FixturesPath, "AlvsResponse.xml"));
    private readonly string _btmsRequestJson = File.ReadAllText(Path.Combine(FixturesPath, "FinalisationNotification.json")).LinuxLineEndings();
    private readonly StringContent _originalRequestSoapContent;

    public FinalisationNotificationTests()
    {
        _originalRequestSoapContent = new StringContent(_originalRequestSoap, Encoding.UTF8, MediaTypeNames.Application.Soap);
        TestWebServer.RoutedHttpHandler.SetNextResponse(content: _originalResponseSoap, statusFunc: () => HttpStatusCode.Accepted);
    }

    [Fact]
    public async Task When_receiving_request_from_cds_Then_should_forward_to_alvs()
    {
        await HttpClient.PostAsync(GatewayPath, _originalRequestSoapContent);

        TestWebServer.RoutedHttpHandler.LastRequest!.RequestUri!.AbsolutePath.Should().Be(OriginalPath);
        (await TestWebServer.RoutedHttpHandler.LastRequest!.Content!.ReadAsStringAsync()).Should().Be(_originalRequestSoap);
    }

    [Fact]
    public async Task When_receiving_request_from_cds_Then_should_respond_with_alvs_response()
    {
        var response = await HttpClient.PostAsync(GatewayPath, _originalRequestSoapContent);

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
        (await response.Content.ReadAsStringAsync()).Should().Be(_originalResponseSoap);
    }

    [Fact]
    public async Task When_receiving_request_from_cds_Then_should_forward_converted_json_to_btms()
    {
        await HttpClient.PostAsync(GatewayPath, _originalRequestSoapContent);

        TestWebServer.ForkedHttpHandler.LastRequest!.RequestUri!.AbsolutePath.Should().Be(BtmsPath);
        (await TestWebServer.ForkedHttpHandler.LastRequest!.Content!.ReadAsStringAsync()).LinuxLineEndings().Should().Be(_btmsRequestJson);
    }
}