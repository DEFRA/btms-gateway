using System.Net;
using BtmsGateway.IntegrationTests.Helpers;
using BtmsGateway.IntegrationTests.TestBase;
using BtmsGateway.IntegrationTests.TestUtils;
using FluentAssertions;
using Xunit.Abstractions;

namespace BtmsGateway.IntegrationTests.Endpoints.Admin;

public class RemoveMessageTests(ITestOutputHelper output) : SqsTestBase(output)
{
    [Fact]
    public async Task When_message_processing_fails_and_moved_to_dlq_Then_message_can_be_removed()
    {
        var resourceEvent = FixtureTest.UsingContent("CustomsDeclarationClearanceDecisionResourceEvent.json");
        const string mrn = "25GB0XX00XXXXX0001";
        resourceEvent = resourceEvent.Replace("25GB0XX00XXXXX0000", mrn);

        await PurgeQueue(ResourceEventsQueueUrl);
        await PurgeQueue(ResourceEventsDeadLetterQueueUrl);

        var messageId = await SendMessage(
            mrn,
            resourceEvent,
            ResourceEventsDeadLetterQueueUrl,
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

        // We expect no messages on either queue following the removal of the single message
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
