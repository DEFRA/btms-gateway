using System.Net;
using BtmsGateway.IntegrationTests.Helpers;
using BtmsGateway.IntegrationTests.TestBase;
using BtmsGateway.IntegrationTests.TestUtils;
using Defra.TradeImportsDataApi.Domain.Events;
using FluentAssertions;
using Xunit.Abstractions;

namespace BtmsGateway.IntegrationTests.Endpoints.Admin;

public class CountTests(ITestOutputHelper output) : SqsTestBase(output)
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
        var response = await httpClient.GetAsync(Testing.Endpoints.Redrive.DeadLetterQueue.Count());

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        response.Content.ReadAsStringAsync().Result.Should().Be("{\"deadLetterQueueCount\":1}");
    }
}
