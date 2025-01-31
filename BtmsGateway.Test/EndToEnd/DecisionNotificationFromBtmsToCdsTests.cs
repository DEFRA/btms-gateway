using System.Net;
using System.Net.Mime;
using System.Text;
using FluentAssertions;

namespace BtmsGateway.Test.EndToEnd;

public class DecisionNotificationFromBtmsToCdsTests : TargetRoutingTestBase
{
    private const string UrlPath = "/route/path/btms-cds/decision-notification";
    
    private readonly string _originalMessageJson = File.ReadAllText(Path.Combine(FixturesPath, "DecisionNotification.json"));
    private readonly string _targetMessageSoap = File.ReadAllText(Path.Combine(FixturesPath, "AlvsToCdsDecisionNotification.xml"));
    private readonly StringContent _originalRequestJsonContent;

    public DecisionNotificationFromBtmsToCdsTests()
    {
        _originalRequestJsonContent = new StringContent(_originalMessageJson, Encoding.UTF8, MediaTypeNames.Application.Json);
        TestWebServer.RoutedHttpHandler.SetNextResponse(statusFunc: () => HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task When_receiving_request_from_btms_Then_should_forward_converted_soap_to_cds()
    {
        await HttpClient.PostAsync(UrlPath, _originalRequestJsonContent);

        TestWebServer.RoutedHttpHandler.LastRequest!.RequestUri!.AbsoluteUri.Should().Be($"http://cds-host{UrlPath}");
        (await TestWebServer.RoutedHttpHandler.LastRequest!.Content!.ReadAsStringAsync()).Should().Be(_targetMessageSoap);
    }

    [Fact]
    public async Task When_receiving_request_from_btms_Then_should_respond_with_cds_response()
    {
        var response = await HttpClient.PostAsync(UrlPath, _originalRequestJsonContent);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await response.Content.ReadAsStringAsync()).Should().Be(string.Empty);
    }

    [Fact]
    public async Task When_receiving_request_from_btms_Then_should_not_forward_to_btms()
    {
        await HttpClient.PostAsync(UrlPath, _originalRequestJsonContent);

        TestWebServer.ForkedHttpHandler.LastRequest.Should().BeNull();
    }
}