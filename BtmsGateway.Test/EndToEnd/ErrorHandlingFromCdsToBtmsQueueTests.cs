using System.Net.Mime;
using System.Text;
using BtmsGateway.Test.TestUtils;
using FluentAssertions;
using Xunit.Abstractions;

namespace BtmsGateway.Test.EndToEnd;

public class ErrorHandlingFromCdsToBtmsQueueTests(ITestOutputHelper testOutputHelper)
    : QueueRoutingTestBase(testOutputHelper, "customs_error_fork.fifo", "customs_error_route.fifo")
{
    private const string ForkPath = "/route/path/cds-btms/error-fork-queue";
    private const string RoutePath = "/route/path/cds-btms/error-route-queue";

    private readonly string _cdsRequestSoap = File.ReadAllText(Path.Combine(FixturesPath, "AlvsErrorHandling.xml"));
    private readonly string _btmsRequestJson = File.ReadAllText(Path.Combine(FixturesPath, "AlvsErrorHandling.json"))
        .LinuxLineEndings();

    [Fact]
    public async Task When_receiving_request_from_cds_Then_should_fork_converted_json_to_btms_queue()
    {
        // Arrange
        var cdsRequestSoapContent = new StringContent(_cdsRequestSoap, Encoding.UTF8, MediaTypeNames.Application.Soap);

        // Act
        await HttpClient.PostAsync(ForkPath, cdsRequestSoapContent);

        // Assert
        var receivedMessages = await GetMessages(ForkQueueName);
        receivedMessages.Should().NotBeEmpty();
        receivedMessages.Should().HaveCount(1);
        receivedMessages.FirstOrDefault().LinuxLineEndings().Should().Be(_btmsRequestJson);
    }

    [Fact]
    public async Task When_receiving_request_from_cds_Then_should_route_converted_json_to_btms_queue()
    {
        // Arrange
        var cdsRequestSoapContent = new StringContent(_cdsRequestSoap, Encoding.UTF8, MediaTypeNames.Application.Soap);

        // Act
        await HttpClient.PostAsync(RoutePath, cdsRequestSoapContent);

        // Assert
        var receivedMessages = await GetMessages(RouteQueueName);
        receivedMessages.Should().NotBeEmpty();
        receivedMessages.Should().HaveCount(1);
        receivedMessages.FirstOrDefault().LinuxLineEndings().Should().Be(_btmsRequestJson);
    }
}
