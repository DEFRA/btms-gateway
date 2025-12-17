using System.Net;
using BtmsGateway.IntegrationTests.Helpers;
using BtmsGateway.IntegrationTests.TestBase;
using BtmsGateway.IntegrationTests.TestUtils;
using BtmsGateway.Utils.Http;
using Defra.TradeImportsDataApi.Domain.Events;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using WireMock.Client;
using WireMock.Client.Extensions;
using Xunit.Abstractions;

namespace BtmsGateway.IntegrationTests.Utils.Http;

/// <summary>
/// This test uses its own dedicated SQS queues, as it launches its own application instance using the IntegrationTests profile which integrates with wiremock, and we need to prevent the consumers from competing with the already running instance
/// </summary>
/// <param name="wireMockClient"></param>
/// <param name="output"></param>
[Collection("UsesWireMockClient")]
public class ProxyTests(WireMockClient wireMockClient, ITestOutputHelper output) : SqsTestBase(output)
{
    private readonly IWireMockAdminApi _wireMockAdminApi = wireMockClient.WireMockAdminApi;

    [Fact]
    public async Task When_post_to_cds_takes_longer_than_http_client_timeout_Then_message_goes_to_dlq()
    {
        await wireMockClient.ResetWiremock();

        await PurgeQueue(IntegrationTestProfileResourceEventsQueueUrl);
        await PurgeQueue(IntegrationTestProfileResourceEventsDeadLetterQueueUrl);

        await using var application = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("IntegrationTests");
        });

        var configuration = application.Services.GetService(typeof(IConfiguration));
        var httpClientTimeoutSeconds = ((ConfigurationManager)configuration!).GetValue<int>("HttpClientTimeoutSeconds");

        var responseDelay =
            httpClientTimeoutSeconds > 0 ? httpClientTimeoutSeconds + 1 : Proxy.DefaultHttpClientTimeoutSeconds + 1;

        var postMappingBuilder = _wireMockAdminApi.GetMappingBuilder();
        postMappingBuilder.Given(m =>
            m.WithRequest(req => req.UsingPost().WithPath("/cds/ws/CDS/defra/alvsclearanceinbound/v1"))
                .WithResponse(rsp =>
                    rsp.WithStatusCode(HttpStatusCode.ServiceUnavailable).WithDelay(TimeSpan.FromSeconds(responseDelay))
                )
        );
        var postStatus = await postMappingBuilder.BuildAndPostAsync();
        Assert.NotNull(postStatus.Guid);

        var resourceEvent = FixtureTest.UsingContent("CustomsDeclarationClearanceDecisionResourceEvent.json");
        const string mrn = "25GB0XX00XXXXX0000";

        await SendMessage(
            mrn,
            resourceEvent,
            IntegrationTestProfileResourceEventsQueueUrl,
            WithResourceEventAttributes<ResourceEvent<CustomsDeclarationEvent>>(
                "CustomsDeclaration",
                "ClearanceDecision",
                mrn
            ),
            false
        );

        Assert.True(
            await AsyncWaiter.WaitForAsync(
                async () =>
                    (
                        await GetQueueAttributes(IntegrationTestProfileResourceEventsDeadLetterQueueUrl)
                    ).ApproximateNumberOfMessages == 1,
                TimeSpan.FromSeconds(35) // Wait longer than visibility timeout and consumer delivery attempt so the message gets moved to DLQ
            )
        );
    }
}
