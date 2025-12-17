using BtmsGateway.IntegrationTests.Data.Entities;
using BtmsGateway.IntegrationTests.Helpers;
using BtmsGateway.IntegrationTests.TestBase;
using BtmsGateway.IntegrationTests.TestUtils;
using Defra.TradeImportsDataApi.Domain.Events;
using MongoDB.Driver;
using Xunit.Abstractions;

namespace BtmsGateway.IntegrationTests.EndToEnd.Errors;

public class OutboundErrorsTests(ITestOutputHelper output) : SqsTestBase(output)
{
    private readonly string _errorResourceEvent = FixtureTest.UsingContent(
        "CustomsDeclarationProcessingErrorResourceEvent.json"
    );
    private readonly string _mrn = "25GB0XX00XXXXX0000";

    [Fact]
    public async Task When_error_notification_event_consumed_from_btms_Then_should_forward_to_cds()
    {
        await DrainAllMessages(ResourceEventsQueueUrl);
        await DrainAllMessages(ResourceEventsDeadLetterQueueUrl);

        var errorNotificationsCollection = GetErrorNotificationsCollection();
        await errorNotificationsCollection.DeleteManyAsync(FilterDefinition<Notification>.Empty);

        await SendMessage(
            _mrn,
            _errorResourceEvent,
            ResourceEventsQueueUrl,
            WithResourceEventAttributes<ResourceEvent<ProcessingErrorEvent>>("ProcessingError", null, _mrn),
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
                var errorNotifications = await errorNotificationsCollection.FindAsync(
                    FilterDefinition<Notification>.Empty
                );
                return errorNotifications.ToList().Count == 1;
            })
        );
    }
}
