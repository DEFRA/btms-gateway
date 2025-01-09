using System.Net;
using System.Net.Mime;
using System.Text;
using FluentAssertions;

namespace BtmsGateway.Test.EndToEnd;

public class ClearanceRequestTests : TargetRoutingTestBase
{
    private const string OriginalClearanceRequestPath = "/clearance-request/path";
    private const string GatewayClearanceRequestPath = $"/cds{OriginalClearanceRequestPath}";
    private const string BtmsClearanceRequestPath = $"/forked{OriginalClearanceRequestPath}";
    
    private readonly string _clearanceRequestSoap = File.ReadAllText(Path.Combine(FixturesPath, "ClearanceRequest.xml"));
    private readonly string _clearanceRequestResponseSoap = File.ReadAllText(Path.Combine(FixturesPath, "ClearanceRequestResponse.xml"));
    private readonly string _clearanceRequestJson = File.ReadAllText(Path.Combine(FixturesPath, "ClearanceRequest.json"));
    private readonly StringContent _clearanceRequestSoapContent;

    public ClearanceRequestTests()
    {
        TestWebServer.RoutedHttpHandler.SetNextResponse(content: _clearanceRequestResponseSoap, statusFunc: () => HttpStatusCode.Accepted);
        _clearanceRequestSoapContent = new StringContent(_clearanceRequestSoap, Encoding.UTF8, MediaTypeNames.Application.Soap);
    }

    [Fact]
    public async Task When_receiving_clearance_request_from_cds_Then_should_forward_to_alvs()
    {
        await HttpClient.PostAsync(GatewayClearanceRequestPath, _clearanceRequestSoapContent);

        TestWebServer.RoutedHttpHandler.LastRequest!.RequestUri!.AbsolutePath.Should().Be(OriginalClearanceRequestPath);
        (await TestWebServer.RoutedHttpHandler.LastRequest!.Content!.ReadAsStringAsync()).Should().Be(_clearanceRequestSoap);
    }

    [Fact]
    public async Task When_receiving_clearance_request_from_cds_Then_should_respond_with_alvs_response()
    {
        var response = await HttpClient.PostAsync(GatewayClearanceRequestPath, _clearanceRequestSoapContent);

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
        (await response.Content.ReadAsStringAsync()).Should().Be(_clearanceRequestResponseSoap);
    }

    [Fact]
    public async Task When_receiving_clearance_request_from_cds_Then_should_forward_converted_json_to_btms()
    {
        await HttpClient.PostAsync(GatewayClearanceRequestPath, _clearanceRequestSoapContent);

        TestWebServer.ForkedHttpHandler.LastRequest!.RequestUri!.AbsolutePath.Should().Be(BtmsClearanceRequestPath);
        (await TestWebServer.ForkedHttpHandler.LastRequest!.Content!.ReadAsStringAsync()).Should().Be(_clearanceRequestJson);
    }
}