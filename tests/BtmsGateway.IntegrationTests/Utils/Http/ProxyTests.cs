using System.Diagnostics.CodeAnalysis;
using System.Net;
using BtmsGateway.IntegrationTests.Helpers;
using BtmsGateway.IntegrationTests.TestBase;
using BtmsGateway.IntegrationTests.TestUtils;
using BtmsGateway.Utils.Http;
using WireMock.Client;
using WireMock.Client.Extensions;
using Xunit.Abstractions;

namespace BtmsGateway.IntegrationTests.Utils.Http;

[Collection("UsesWireMockClient")]
public class ProxyTests(WireMockClient wireMockClient, ITestOutputHelper output) : SqsTestBase(output)
{
    private readonly IWireMockAdminApi _wireMockAdminApi = wireMockClient.WireMockAdminApi;

    private readonly string _decisionResourceEvent = FixtureTest.UsingContent(
        "CustomsDeclarationClearanceDecisionResourceEvent.json"
    );
    private readonly string _decisionNotification = FixtureTest.UsingContent("DecisionNotification.xml");
    private readonly string _mrn = "25GB0XX00XXXXX0000";

    [Fact(
        Skip = "Changes in Endpoints > Admin integration tests have now stopped this test from work. I cannot "
            + "see how this would have worked since PR https://github.com/DEFRA/btms-gateway/pull/171 was merged "
            + "that replaced WireMock acting as CDS with the CDS simulator. Would like to discuss with someone. As "
            + "the same MRN of 25GB0XX00XXXXX0000 was being used, I think the RedriveTests fixture left a message floating "
            + "that ended up on the DLQ and resulted in this test passing."
    )]
    [SuppressMessage("Usage", "xUnit1004:Test methods should not be skipped")]
    public async Task When_event_processed_and_post_to_cds_takes_longer_than_http_client_timeout_Then_message_is_moved_to_dlq()
    {
        await wireMockClient.ResetWiremock();

        var putMappingBuilder = _wireMockAdminApi.GetMappingBuilder();
        putMappingBuilder.Given(m =>
            m.WithRequest(req => req.UsingPut().WithPath($"/comparer/btms-decisions/{_mrn}"))
                .WithResponse(rsp => rsp.WithStatusCode(HttpStatusCode.OK).WithBody(_decisionNotification))
        );
        var putStatus = await putMappingBuilder.BuildAndPostAsync();
        Assert.NotNull(putStatus.Guid);

        var postMappingBuilder = _wireMockAdminApi.GetMappingBuilder();
        postMappingBuilder.Given(m =>
            m.WithRequest(req => req.UsingPost().WithPath("/cds/ws/CDS/defra/alvsclearanceinbound/v1"))
                .WithResponse(rsp =>
                    rsp.WithStatusCode(HttpStatusCode.ServiceUnavailable)
                        .WithDelay(TimeSpan.FromSeconds(Proxy.DefaultHttpClientTimeoutSeconds + 1))
                )
        );
        var postStatus = await postMappingBuilder.BuildAndPostAsync();
        Assert.NotNull(postStatus.Guid);

        await DrainAllMessages(ResourceEventsQueueUrl);
        await DrainAllMessages(ResourceEventsDeadLetterQueueUrl);

        await SendMessage(
            _mrn,
            _decisionResourceEvent,
            ResourceEventsQueueUrl,
            WithResourceEventAttributes("CustomsDeclaration", "ClearanceDecision", _mrn),
            false
        );

        Assert.True(
            await AsyncWaiter.WaitForAsync(async () =>
                (await GetQueueAttributes(ResourceEventsDeadLetterQueueUrl)).ApproximateNumberOfMessages == 1
            ),
            "ProxyTest message was not moved to DLQ"
        );
    }
}
