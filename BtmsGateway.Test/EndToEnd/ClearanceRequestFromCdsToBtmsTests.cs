using System.Net;
using System.Net.Mime;
using System.Text;
using BtmsGateway.Test.TestUtils;
using FluentAssertions;

namespace BtmsGateway.Test.EndToEnd;

public class ClearanceRequestFromCdsToBtmsTests : TargetRoutingTestBase
{
    private const string UrlPath = "/route/path/cds-btms/clearance-request";
    
    private readonly string _cdsRequestSoap = File.ReadAllText(Path.Combine(FixturesPath, "CdsToAlvsClearanceRequest.xml"));
    private readonly string _cdsResponseSoap = File.ReadAllText(Path.Combine(FixturesPath, "AlvsResponse.xml"));
    private readonly string _btmsRequestJson = File.ReadAllText(Path.Combine(FixturesPath, "ClearanceRequest.json")).LinuxLineEndings();
    private readonly StringContent _cdsRequestSoapContent;

    public ClearanceRequestFromCdsToBtmsTests()
    {
        _cdsRequestSoapContent = new StringContent(_cdsRequestSoap, Encoding.UTF8, MediaTypeNames.Application.Soap);
        TestWebServer.RoutedHttpHandler.SetNextResponse(content: _cdsResponseSoap, statusFunc: () => HttpStatusCode.Accepted);
    }

    [Fact]
    public async Task When_receiving_request_from_cds_Then_should_forward_converted_json_to_btms()
    {
        await HttpClient.PostAsync(UrlPath, _cdsRequestSoapContent);

        TestWebServer.RoutedHttpHandler.LastRequest!.RequestUri!.AbsoluteUri.Should().Be($"http://btms-host{UrlPath}");
        (await TestWebServer.RoutedHttpHandler.LastRequest!.Content!.ReadAsStringAsync()).LinuxLineEndings().Should().Be(_btmsRequestJson);
    }

    [Fact]
    public async Task When_receiving_request_from_cds_Then_should_respond_with_btms_response()
    {
        var response = await HttpClient.PostAsync(UrlPath, _cdsRequestSoapContent);

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
        (await response.Content.ReadAsStringAsync()).Should().Be(_cdsResponseSoap);
    }

    [Fact]
    public async Task When_receiving_request_from_cds_Then_should_not_forward_to_btms()
    {
        await HttpClient.PostAsync(UrlPath, _cdsRequestSoapContent);

        TestWebServer.ForkedHttpHandler.LastRequest.Should().BeNull();
    }
}