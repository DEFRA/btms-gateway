using System.Net;
using System.Net.Mime;
using System.Text;
using BtmsGateway.Test.TestUtils;
using FluentAssertions;

namespace BtmsGateway.Test.EndToEnd;

public class ErrorHandlingFromAlvsToCdsTests : TargetRoutingTestBase
{
    private const string UrlPath = "/route/path/alvs-cds/error-notification";

    private readonly string _alvsRequestSoap = File.ReadAllText(Path.Combine(FixturesPath, "CdsErrorHandling.xml"));
    private readonly StringContent _alvsRequestSoapContent;

    public ErrorHandlingFromAlvsToCdsTests()
    {
        _alvsRequestSoapContent = new StringContent(_alvsRequestSoap, Encoding.UTF8, MediaTypeNames.Application.Soap);
        TestWebServer.RoutedHttpHandler.SetNextResponse(statusFunc: () => HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task When_receiving_request_from_alvs_Then_should_forward_to_cds()
    {
        await HttpClient.PostAsync(UrlPath, _alvsRequestSoapContent);

        TestWebServer
            .RoutedHttpHandler.LastRequest!.RequestUri!.AbsoluteUri.Should()
            .Be($"http://alvs-cds-host{UrlPath}");
        (await TestWebServer.RoutedHttpHandler.LastRequest!.Content!.ReadAsStringAsync()).Should().Be(_alvsRequestSoap);
    }

    [Fact]
    public async Task When_receiving_request_from_alvs_Then_should_respond_with_cds_response()
    {
        var response = await HttpClient.PostAsync(UrlPath, _alvsRequestSoapContent);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await response.Content.ReadAsStringAsync()).Should().Be(string.Empty);
    }

    [Fact]
    public async Task When_receiving_request_from_alvs_Then_should_forward_decision_to_decision_comparer()
    {
        await HttpClient.PostAsync(UrlPath, _alvsRequestSoapContent);

        TestWebServer
            .DecisionComparerClientWithRetryHttpHandler.LastRequest!.RequestUri!.AbsoluteUri.Should()
            .Be($"http://trade-imports-decision-comparer-host/alvs-outbound-errors/25GB2Q3M9H9K5MSAR8");
        (await TestWebServer.DecisionComparerClientWithRetryHttpHandler.LastRequest!.Content!.ReadAsStringAsync())
            .LinuxLineEndings()
            .Should()
            .Be(_alvsRequestSoap);
    }
}
