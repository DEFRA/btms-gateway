using System.Net;
using BtmsGateway.IntegrationTests.Helpers;
using BtmsGateway.IntegrationTests.TestBase;
using BtmsGateway.IntegrationTests.TestUtils;
using Defra.TradeImportsDataApi.Domain.Events;
using FluentAssertions;
using Xunit.Abstractions;

namespace BtmsGateway.IntegrationTests.Endpoints.Admin;

public class DrainTests(ITestOutputHelper output) : SqsTestBase(output)
{
    [Fact]
    public async Task When_message_processing_fails_and_moved_to_dlq_Then_dlq_can_be_drained()
    {
        var resourceEvent = FixtureTest.UsingContent("CustomsDeclarationClearanceDecisionResourceEvent.json");
        const string mrn = "25GB0XX00XXXXX0002";
        resourceEvent = resourceEvent.Replace("25GB0XX00XXXXX0000", mrn);

        await PurgeQueue(ResourceEventsQueueUrl);
        await PurgeQueue(ResourceEventsDeadLetterQueueUrl);

        await SendMessage(
            mrn,
            resourceEvent,
            ResourceEventsDeadLetterQueueUrl,
            WithResourceEventAttributes<ResourceEvent<CustomsDeclarationEvent>>(
                "CustomsDeclaration",
                "ClearanceDecision",
                mrn
            ),
            false
        );

        var messagesOnDeadLetterQueue = await AsyncWaiter.WaitForAsync(async () =>
            (await GetQueueAttributes(ResourceEventsDeadLetterQueueUrl)).ApproximateNumberOfMessages == 1
        );
        Assert.True(messagesOnDeadLetterQueue, "Messages on dead letter queue was not received");

        var httpClient = CreateHttpClient();
        var response = await httpClient.PostAsync(Testing.Endpoints.Redrive.DeadLetterQueue.Drain(), null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // We expect no messages on either queue following a drain
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
