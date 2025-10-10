using BtmsGateway.IntegrationTests.Data.Entities;
using BtmsGateway.IntegrationTests.Helpers;
using BtmsGateway.IntegrationTests.TestBase;
using BtmsGateway.IntegrationTests.TestUtils;
using MongoDB.Driver;
using Xunit.Abstractions;

namespace BtmsGateway.IntegrationTests.EndToEnd.Decisions;

[Collection("UsesWireMockClient")]
public class DecisionsTests(WireMockClient wireMockClient, ITestOutputHelper output) : SqsTestBase(output)
{
    private readonly string _decisionResourceEvent = FixtureTest.UsingContent(
        "CustomsDeclarationClearanceDecisionResourceEvent.json"
    );
    private readonly string _mrn = "25GB0XX00XXXXX0000";

    [Fact]
    public async Task When_decision_notification_event_consumed_from_btms_Then_should_forward_to__cds()
    {
        await DrainAllMessages(ResourceEventsQueueUrl);
        await DrainAllMessages(ResourceEventsDeadLetterQueueUrl);

        var decisionNotificationsCollection = GetDecisionNotificationsCollection();
        await decisionNotificationsCollection.DeleteManyAsync(FilterDefinition<Notification>.Empty);

        await SendMessage(
            _mrn,
            _decisionResourceEvent,
            ResourceEventsQueueUrl,
            WithResourceEventAttributes("CustomsDeclaration", "ClearanceDecision", _mrn),
            false
        );

        Assert.True(
            await AsyncWaiter.WaitForAsync(async () =>
                (await GetQueueAttributes(ResourceEventsQueueUrl)).ApproximateNumberOfMessages == 0
                && (await GetQueueAttributes(ResourceEventsQueueUrl)).ApproximateNumberOfMessagesNotVisible == 0
            )
        );
        Assert.True(
            await AsyncWaiter.WaitForAsync(async () =>
            {
                var decisionNotifications = await decisionNotificationsCollection.FindAsync(
                    FilterDefinition<Notification>.Empty
                );
                return decisionNotifications.ToList().Count == 1;
            })
        );
    }
}
