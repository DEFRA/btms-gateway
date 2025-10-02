using System.Net;
using BtmsGateway.IntegrationTests.Data.Entities;
using BtmsGateway.IntegrationTests.Helpers;
using BtmsGateway.IntegrationTests.TestBase;
using BtmsGateway.IntegrationTests.TestUtils;
using MongoDB.Driver;
using WireMock.Client;
using WireMock.Client.Extensions;
using Xunit.Abstractions;

namespace BtmsGateway.IntegrationTests.EndToEnd.Errors;

[Collection("UsesWireMockClient")]
public class OutboundErrorsTests(WireMockClient wireMockClient, ITestOutputHelper output) : SqsTestBase(output)
{
    private readonly IWireMockAdminApi _wireMockAdminApi = wireMockClient.WireMockAdminApi;

    private readonly string _errorResourceEvent = FixtureTest.UsingContent(
        "CustomsDeclarationProcessingErrorResourceEvent.json"
    );
    private readonly string _errorNotification = FixtureTest.UsingContent("ErrorNotification.xml");
    private readonly string _mrn = "25GB0XX00XXXXX0000";

    [Fact]
    public async Task When_error_notification_event_consumed_from_btms_Then_should_forward_to_decision_comparer_and_cds()
    {
        var comparerRequestPath = $"/comparer/btms-outbound-errors/{_mrn}";
        var putMappingBuilder = _wireMockAdminApi.GetMappingBuilder();
        putMappingBuilder.Given(m =>
            m.WithRequest(req => req.UsingPut().WithPath(comparerRequestPath))
                .WithResponse(rsp => rsp.WithStatusCode(HttpStatusCode.OK).WithBody(_errorNotification))
        );
        var putStatus = await putMappingBuilder.BuildAndPostAsync();
        Assert.NotNull(putStatus.Guid);

        await DrainAllMessages(ResourceEventsQueueUrl);
        await DrainAllMessages(ResourceEventsDeadLetterQueueUrl);

        var errorNotificationsCollection = GetErrorNotificationsCollection();
        await errorNotificationsCollection.DeleteManyAsync(FilterDefinition<Notification>.Empty);

        await SendMessage(
            _mrn,
            _errorResourceEvent,
            ResourceEventsQueueUrl,
            WithResourceEventAttributes("ProcessingError", null, _mrn),
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
                (await _wireMockAdminApi.GetRequestsAsync()).Any(logEntry =>
                    logEntry.Request.Path == comparerRequestPath
                    && logEntry.Request.Method == HttpMethod.Put.Method
                    && logEntry.Request.Body == _errorNotification
                )
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
