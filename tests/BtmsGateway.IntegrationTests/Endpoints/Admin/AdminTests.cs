using System.Net;
using BtmsGateway.IntegrationTests.Helpers;
using BtmsGateway.IntegrationTests.TestBase;
using BtmsGateway.IntegrationTests.TestUtils;
using FluentAssertions;
using WireMock.Client;
using WireMock.Client.Extensions;
using Xunit.Abstractions;

namespace BtmsGateway.IntegrationTests.Endpoints.Admin;

[Collection("UsesWireMockClient")]
public class AdminIntegrationTests(WireMockClient wireMockClient, ITestOutputHelper output) : SqsTestBase(output)
{
    private readonly IWireMockAdminApi _wireMockAdminApi = wireMockClient.WireMockAdminApi;

    [Fact]
    public async Task When_message_processing_fails_and_moved_to_dlq_Then_message_can_be_redriven()
    {
        var resourceEvent = FixtureTest.UsingContent("CustomsDeclarationClearanceDecisionResourceEvent.json");
        var decisionNotification = FixtureTest.UsingContent("DecisionNotification.xml");
        var mrn = "25GB0XX00XXXXX0000";

        var putMappingBuilder = _wireMockAdminApi.GetMappingBuilder();
        putMappingBuilder.Given(m =>
            m.WithRequest(req => req.UsingPut().WithPath("/comparer/btms-decisions/" + mrn))
                .WithResponse(rsp => rsp.WithStatusCode(HttpStatusCode.OK).WithBody(decisionNotification))
        );
        var putStatus = await putMappingBuilder.BuildAndPostAsync();
        Assert.NotNull(putStatus.Guid);

        // Configure failure responses from CDS (including retries) so the message gets moved to DLQ and then successful on redrive
        var failFirstPostMappingBuilder = _wireMockAdminApi.GetMappingBuilder();
        failFirstPostMappingBuilder.Given(m =>
            m.WithRequest(req => req.UsingPost().WithPath("/cds/ws/CDS/defra/alvsclearanceinbound/v1"))
                .WithScenario("DLQ Redrive")
                .WithSetStateTo("CDS First Failure")
                .WithResponse(rsp => rsp.WithStatusCode(HttpStatusCode.ServiceUnavailable))
        );
        var postFailStatus = await failFirstPostMappingBuilder.BuildAndPostAsync();
        Assert.NotNull(postFailStatus.Guid);

        var failRetry1PostMappingBuilder = _wireMockAdminApi.GetMappingBuilder();
        failRetry1PostMappingBuilder.Given(m =>
            m.WithRequest(req => req.UsingPost().WithPath("/cds/ws/CDS/defra/alvsclearanceinbound/v1"))
                .WithScenario("DLQ Redrive")
                .WithWhenStateIs("CDS First Failure")
                .WithSetStateTo("CDS Retry 1 Failure")
                .WithResponse(rsp => rsp.WithStatusCode(HttpStatusCode.ServiceUnavailable))
        );
        var postFailRetry1Status = await failRetry1PostMappingBuilder.BuildAndPostAsync();
        Assert.NotNull(postFailRetry1Status.Guid);

        var failRetry2PostMappingBuilder = _wireMockAdminApi.GetMappingBuilder();
        failRetry2PostMappingBuilder.Given(m =>
            m.WithRequest(req => req.UsingPost().WithPath("/cds/ws/CDS/defra/alvsclearanceinbound/v1"))
                .WithScenario("DLQ Redrive")
                .WithWhenStateIs("CDS Retry 1 Failure")
                .WithSetStateTo("CDS Retry 2 Failure")
                .WithResponse(rsp => rsp.WithStatusCode(HttpStatusCode.ServiceUnavailable))
        );
        var postFailRetry2Status = await failRetry2PostMappingBuilder.BuildAndPostAsync();
        Assert.NotNull(postFailRetry2Status.Guid);

        var failRetry3PostMappingBuilder = _wireMockAdminApi.GetMappingBuilder();
        failRetry3PostMappingBuilder.Given(m =>
            m.WithRequest(req => req.UsingPost().WithPath("/cds/ws/CDS/defra/alvsclearanceinbound/v1"))
                .WithScenario("DLQ Redrive")
                .WithWhenStateIs("CDS Retry 2 Failure")
                .WithSetStateTo("CDS Retry 3 Failure")
                .WithResponse(rsp => rsp.WithStatusCode(HttpStatusCode.ServiceUnavailable))
        );
        var postFailRetry3Status = await failRetry3PostMappingBuilder.BuildAndPostAsync();
        Assert.NotNull(postFailRetry3Status.Guid);

        var successfulPostMappingBuilder = _wireMockAdminApi.GetMappingBuilder();
        successfulPostMappingBuilder.Given(m =>
            m.WithRequest(req => req.UsingPost().WithPath("/cds/ws/CDS/defra/alvsclearanceinbound/v1"))
                .WithScenario("DLQ Redrive")
                .WithWhenStateIs("CDS Retry 3 Failure")
                .WithSetStateTo("CDS Back Online")
                .WithResponse(rsp => rsp.WithStatusCode(HttpStatusCode.NoContent))
        );
        var postSuccessStatus = await successfulPostMappingBuilder.BuildAndPostAsync();
        Assert.NotNull(postSuccessStatus.Guid);

        await _wireMockAdminApi.ResetScenarioAsync("DLQ Redrive");

        await DrainAllMessages(ResourceEventsQueueUrl);
        await DrainAllMessages(ResourceEventsDeadLetterQueueUrl);

        await SendMessage(
            mrn,
            resourceEvent,
            ResourceEventsQueueUrl,
            WithResourceEventAttributes("CustomsDeclaration", "ClearanceDecision", mrn),
            false
        );

        var messagesOnDeadLetterQueue = await AsyncWaiter.WaitForAsync(
            async () => (await GetQueueAttributes(ResourceEventsDeadLetterQueueUrl)).ApproximateNumberOfMessages == 1,
            TimeSpan.FromSeconds(35) // Wait a bit longer than message visibility timeout so the message gets moved to DLQ
        );
        Assert.True(messagesOnDeadLetterQueue, "Messages on dead letter queue was not received");

        var httpClient = CreateHttpClient();
        var response = await httpClient.PostAsync(Testing.Endpoints.AdminIntegration.PostRedrive(), null);

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
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
