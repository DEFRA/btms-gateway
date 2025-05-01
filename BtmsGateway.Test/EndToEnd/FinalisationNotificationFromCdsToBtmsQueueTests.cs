using System.Net.Mime;
using System.Text;
using BtmsGateway.Test.TestUtils;
using FluentAssertions;
using Xunit.Abstractions;

namespace BtmsGateway.Test.EndToEnd;

public class FinalisationNotificationFromCdsToBtmsQueueTests(ITestOutputHelper testOutputHelper)
    : QueueRoutingTestBase(testOutputHelper, "customs_finalisation_fork.fifo", "customs_finalisation_route.fifo")
{
    private const string ForkPath = "/route/path/cds-btms/finalisation-fork-queue";
    private const string RoutePath = "/route/path/cds-btms/finalisation-route-queue";

    private readonly string _cdsRequestSoap = File.ReadAllText(
        Path.Combine(FixturesPath, "CdsToAlvsFinalisationNotification.xml")
    );
    private readonly string _btmsRequestJson = File.ReadAllText(
            Path.Combine(FixturesPath, "FinalisationNotification.json")
        )
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
