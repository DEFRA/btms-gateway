using System.Net;
using System.Net.Http.Headers;
using BtmsGateway.IntegrationTests.Helpers;
using BtmsGateway.IntegrationTests.TestBase;
using BtmsGateway.IntegrationTests.TestUtils;
using FluentAssertions;
using WireMock.Client;
using WireMock.Client.Extensions;
using Xunit.Abstractions;

namespace BtmsGateway.IntegrationTests.EndToEnd.Errors;

[Collection("UsesWireMockClient")]
public class InboundErrorsTests(WireMockClient wireMockClient, ITestOutputHelper output) : SqsTestBase(output)
{
    private readonly IWireMockAdminApi _wireMockAdminApi = wireMockClient.WireMockAdminApi;
    private readonly string _inboundError = FixtureTest
        .UsingContent("InboundErrorTemplate.xml")
        .WithRandomCorrelationId();

    [Fact]
    public async Task When_receiving_inbound_error_from_cds_Then_should_forward_and_respond_ok()
    {
        var postMappingBuilder = _wireMockAdminApi.GetMappingBuilder();
        postMappingBuilder.Given(m =>
            m.WithRequest(req => req.UsingPost().WithPath($"/alvs{Testing.Endpoints.Errors.PostInboundError()}"))
                .WithResponse(rsp => rsp.WithStatusCode(HttpStatusCode.OK))
        );
        var postStatus = await postMappingBuilder.BuildAndPostAsync();
        Assert.NotNull(postStatus.Guid);

        await PurgeQueue(InboundCustomsDeclarationProcessorQueueUrl);

        var httpClient = CreateHttpClient(false);
        var response = await httpClient.PostAsync(
            Testing.Endpoints.Errors.PostInboundError(),
            new StringContent(_inboundError, new MediaTypeHeaderValue("application/xml"))
        );

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType.Should().Be(new MediaTypeHeaderValue("application/xml"));
        var verifyResponseSettings = new VerifySettings();
        verifyResponseSettings.UseTextForParameters("GatewayResponse");
        await VerifyXml(await response.Content.ReadAsStringAsync(), verifyResponseSettings);

        var mockReceivedRequests = await _wireMockAdminApi.GetRequestsAsync();
        Assert.Contains(
            mockReceivedRequests,
            logEntry =>
                logEntry.Request.Path == $"/alvs{Testing.Endpoints.Errors.PostInboundError()}"
                && logEntry.Request.Method == HttpMethod.Post.Method
                && logEntry.Request.Body == _inboundError
        );

        Assert.True(
            await AsyncWaiter.WaitForAsync(async () =>
                (await GetQueueAttributes(InboundCustomsDeclarationProcessorQueueUrl)).ApproximateNumberOfMessages == 1
            )
        );
        var receiveMessagesResponse = await ReceiveMessage(InboundCustomsDeclarationProcessorQueueUrl);
        receiveMessagesResponse.Messages.Should().HaveCount(1);
        var verifyQueueMessageSettings = new VerifySettings();
        verifyQueueMessageSettings.UseTextForParameters("QueueMessage");
        verifyQueueMessageSettings.DontScrubDateTimes();
        verifyQueueMessageSettings.ScrubMember("correlationId");
        await VerifyJson(receiveMessagesResponse.Messages[0].Body, verifyQueueMessageSettings);
    }
}
