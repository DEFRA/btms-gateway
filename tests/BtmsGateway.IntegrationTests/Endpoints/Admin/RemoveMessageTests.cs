using System.Diagnostics.CodeAnalysis;
using System.Net;
using BtmsGateway.IntegrationTests.Helpers;
using BtmsGateway.IntegrationTests.TestUtils;
using FluentAssertions;
using WireMock.Client;
using Xunit.Abstractions;

namespace BtmsGateway.IntegrationTests.Endpoints.Admin;

[Collection("UsesWireMockClient")]
public class RemoveMessageTests(WireMockClient wireMockClient, ITestOutputHelper output) : AdminTestBase(output)
{
    private readonly IWireMockAdminApi _wireMockAdminApi = wireMockClient.WireMockAdminApi;

    [Fact(
        Skip = "Now the comparer is gone and wiremock isn't used as the CDS sim, then there is no way to force errors, need to chat about this"
    )]
    [SuppressMessage("Usage", "xUnit1004:Test methods should not be skipped")]
    public async Task When_message_processing_fails_and_moved_to_dlq_Then_message_can_be_removed()
    {
        var resourceEvent = FixtureTest.UsingContent("CustomsDeclarationClearanceDecisionResourceEvent.json");
        const string mrn = "25GB0XX00XXXXX0001";
        resourceEvent = resourceEvent.Replace("25GB0XX00XXXXX0000", mrn);

        await SetUpConsumptionFailure(_wireMockAdminApi, "DLQ Remove Message", mrn);
        await DrainAllMessages(ResourceEventsQueueUrl);
        await DrainAllMessages(ResourceEventsDeadLetterQueueUrl);

        var messageId = await SendMessage(
            mrn,
            resourceEvent,
            ResourceEventsQueueUrl,
            WithResourceEventAttributes("CustomsDeclaration", "ClearanceDecision", mrn),
            false
        );

        var messagesOnDeadLetterQueue = await AsyncWaiter.WaitForAsync(
            async () => (await GetQueueAttributes(ResourceEventsDeadLetterQueueUrl)).ApproximateNumberOfMessages == 1,
            TimeSpan.FromMinutes(2)
        );
        Assert.True(messagesOnDeadLetterQueue, "Messages on dead letter queue was not received");

        var httpClient = CreateHttpClient();
        var response = await httpClient.PostAsync(
            Testing.Endpoints.Redrive.DeadLetterQueue.RemoveMessage(messageId),
            null
        );

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // We expect no messages on either queue following removal of the single message
        Assert.True(
            await AsyncWaiter.WaitForAsync(async () =>
                (await GetQueueAttributes(ResourceEventsQueueUrl)).ApproximateNumberOfMessages == 0
            )
        );
        Assert.True(
            await AsyncWaiter.WaitForAsync(async () =>
                (await GetQueueAttributes(ResourceEventsDeadLetterQueueUrl)).ApproximateNumberOfMessages == 0
            )
        );
    }
}
