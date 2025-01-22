using System.Net;
using System.Net.Mime;
using System.Text;
using FluentAssertions;

namespace BtmsGateway.Test.EndToEnd;

public class DecisionNotificationFromBtmsToCdsTests : TargetRoutingTestBase
{
    private const string OriginalPath = "/decision-notification/path";
    private const string GatewayPath = $"/btms-cds{OriginalPath}";
    
    private readonly string _originalRequestSoap = File.ReadAllText(Path.Combine(FixturesPath, "DecisionNotification.xml"));
    private readonly StringContent _originalRequestSoapContent;

    public DecisionNotificationFromBtmsToCdsTests()
    {
        _originalRequestSoapContent = new StringContent(_originalRequestSoap, Encoding.UTF8, MediaTypeNames.Application.Soap);
        TestWebServer.RoutedHttpHandler.SetNextResponse(statusFunc: () => HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task When_receiving_request_from_btms_Then_should_forward_converted_soap_to_cds()
    {
        await HttpClient.PostAsync(GatewayPath, _originalRequestSoapContent);   // TODO: Should be passing in JSON

        TestWebServer.RoutedHttpHandler.LastRequest!.RequestUri!.AbsoluteUri.Should().Be($"http://cds{OriginalPath}");
        (await TestWebServer.RoutedHttpHandler.LastRequest!.Content!.ReadAsStringAsync()).Should().Be(_originalRequestSoap);
    }

    [Fact]
    public async Task When_receiving_request_from_btms_Then_should_respond_with_cds_response()
    {
        var response = await HttpClient.PostAsync(GatewayPath, _originalRequestSoapContent);   // TODO: Should be passing in JSON

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await response.Content.ReadAsStringAsync()).Should().Be(string.Empty);
    }

    [Fact]
    public async Task When_receiving_request_from_btms_Then_should_not_forward_to_btms()
    {
        await HttpClient.PostAsync(GatewayPath, _originalRequestSoapContent);

        TestWebServer.ForkedHttpHandler.LastRequest.Should().BeNull();
    }
}