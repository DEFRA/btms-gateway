using System.Net.Mime;
using System.Text;
using BtmsGateway.Test.TestUtils;
using FluentAssertions;
using Xunit.Abstractions;

namespace BtmsGateway.Test.EndToEnd;

public class ErrorHandlingFromAlvsToBtmsQueueTests(ITestOutputHelper testOutputHelper) : QueueRoutingTestBase(testOutputHelper, "alvs_error_fork.fifo", "alvs_error_route.fifo")
{
    private const string ForkPath = "/route/path/alvs-btms/error-fork-queue";
    private const string RoutePath = "/route/path/alvs-btms/error-route-queue";

    private readonly string _alvsRequestSoap = File.ReadAllText(Path.Combine(FixturesPath, "CdsErrorHandling.xml"));
    private readonly string _btmsRequestJson = File.ReadAllText(Path.Combine(FixturesPath, "CdsErrorHandling.json")).LinuxLineEndings();

    [Fact]
    public async Task When_receiving_request_from_alvs_Then_should_fork_converted_json_to_btms_queue()
    {
        // Arrange 
        var alvsRequestSoapContent = new StringContent(_alvsRequestSoap, Encoding.UTF8, MediaTypeNames.Application.Soap);

        // Act
        await HttpClient.PostAsync(ForkPath, alvsRequestSoapContent);

        // Assert
        var receivedMessages = await GetMessages(ForkQueueName);
        receivedMessages.Should().NotBeEmpty();
        receivedMessages.Should().HaveCount(1);
        receivedMessages.FirstOrDefault().LinuxLineEndings().Should().Be(_btmsRequestJson);
    }

    [Fact]
    public async Task When_receiving_request_from_alvs_Then_should_route_converted_json_to_btms_queue()
    {
        // Arrange 
        var alvsRequestSoapContent = new StringContent(_alvsRequestSoap, Encoding.UTF8, MediaTypeNames.Application.Soap);

        // Act
        await HttpClient.PostAsync(RoutePath, alvsRequestSoapContent);

        // Assert
        var receivedMessages = await GetMessages(RouteQueueName);
        receivedMessages.Should().NotBeEmpty();
        receivedMessages.Should().HaveCount(1);
        receivedMessages.FirstOrDefault().LinuxLineEndings().Should().Be(_btmsRequestJson);
    }
}