using System.Net;
using BtmsGateway.IntegrationTests.Helpers;
using BtmsGateway.IntegrationTests.TestUtils;
using FluentAssertions;
using WireMock.Client;
using Xunit.Abstractions;

namespace BtmsGateway.IntegrationTests.Endpoints.Admin;

[Collection("UsesWireMockClient")]
public class RedriveTests(WireMockClient wireMockClient, ITestOutputHelper output) : AdminTestBase(output)
{
    private readonly IWireMockAdminApi _wireMockAdminApi = wireMockClient.WireMockAdminApi;

    [Fact]
    public async Task When_message_processing_fails_and_moved_to_dlq_Then_message_can_be_redriven()
    {
        var resourceEvent = FixtureTest.UsingContent("CustomsDeclarationClearanceDecisionResourceEvent.json");
        const string mrn = "25GB0XX00XXXXX0000";

        await SetUpConsumptionFailure(_wireMockAdminApi, "DLQ Redrive", mrn);
        await DrainAllMessages(ResourceEventsQueueUrl);
        await DrainAllMessages(ResourceEventsDeadLetterQueueUrl);

        await SendMessage(
            mrn,
            resourceEvent,
            ResourceEventsQueueUrl,
            WithResourceEventAttributes("CustomsDeclaration", "ClearanceDecision", mrn),
            false
        );

        var messagesOnDeadLetterQueue = await AsyncWaiter.WaitForAsync(async () =>
            (await GetQueueAttributes(ResourceEventsDeadLetterQueueUrl)).ApproximateNumberOfMessages == 1
        );
        Assert.True(messagesOnDeadLetterQueue, "Messages on dead letter queue was not received");

        var httpClient = CreateHttpClient();
        var response = await httpClient.PostAsync(Testing.Endpoints.Redrive.DeadLetterQueue.Redrive(), null);

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);

        // First, we expect nothing on the DLQ
        Assert.True(
            await AsyncWaiter.WaitForAsync(async () =>
                (await GetQueueAttributes(ResourceEventsDeadLetterQueueUrl)).ApproximateNumberOfMessages == 0
            )
        );

        // The same message will error again, so we have 1 message on the DLQ
        Assert.True(
            await AsyncWaiter.WaitForAsync(async () =>
                (await GetQueueAttributes(ResourceEventsDeadLetterQueueUrl)).ApproximateNumberOfMessages == 1
            )
        );

        // Drain again and await nothing
        await DrainAllMessages(ResourceEventsDeadLetterQueueUrl);
        Assert.True(
            await AsyncWaiter.WaitForAsync(async () =>
                (await GetQueueAttributes(ResourceEventsDeadLetterQueueUrl)).ApproximateNumberOfMessages == 0
            )
        );
    }
}
