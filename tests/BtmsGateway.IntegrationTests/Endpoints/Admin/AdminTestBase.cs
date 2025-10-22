using System.Net;
using BtmsGateway.IntegrationTests.TestBase;
using WireMock.Client;
using WireMock.Client.Extensions;
using Xunit.Abstractions;

namespace BtmsGateway.IntegrationTests.Endpoints.Admin;

public class AdminTestBase(ITestOutputHelper output) : SqsTestBase(output)
{
    protected static async Task SetUpConsumptionFailure(IWireMockAdminApi wireMockAdminApi, string name, string mrn)
    {
        // Configure failure responses from Comparer (including retries) so the message gets moved to DLQ and then successful on redrive
        var failFirstPostMappingBuilder = wireMockAdminApi.GetMappingBuilder();
        failFirstPostMappingBuilder.Given(m =>
            m.WithRequest(req => req.UsingPut().WithPath("/comparer/btms-decisions/" + mrn))
                .WithScenario(name)
                .WithSetStateTo("Comparer First Failure")
                .WithResponse(rsp => rsp.WithStatusCode(HttpStatusCode.ServiceUnavailable))
        );
        var postFailStatus = await failFirstPostMappingBuilder.BuildAndPostAsync();
        Assert.NotNull(postFailStatus.Guid);

        var failRetry1PostMappingBuilder = wireMockAdminApi.GetMappingBuilder();
        failRetry1PostMappingBuilder.Given(m =>
            m.WithRequest(req => req.UsingPut().WithPath("/comparer/btms-decisions/" + mrn))
                .WithScenario(name)
                .WithWhenStateIs("Comparer First Failure")
                .WithSetStateTo("Comparer Retry 1 Failure")
                .WithResponse(rsp => rsp.WithStatusCode(HttpStatusCode.ServiceUnavailable))
        );
        var postFailRetry1Status = await failRetry1PostMappingBuilder.BuildAndPostAsync();
        Assert.NotNull(postFailRetry1Status.Guid);

        var failRetry2PostMappingBuilder = wireMockAdminApi.GetMappingBuilder();
        failRetry2PostMappingBuilder.Given(m =>
            m.WithRequest(req => req.UsingPut().WithPath("/comparer/btms-decisions/" + mrn))
                .WithScenario(name)
                .WithWhenStateIs("Comparer Retry 1 Failure")
                .WithSetStateTo("Comparer Retry 2 Failure")
                .WithResponse(rsp => rsp.WithStatusCode(HttpStatusCode.ServiceUnavailable))
        );
        var postFailRetry2Status = await failRetry2PostMappingBuilder.BuildAndPostAsync();
        Assert.NotNull(postFailRetry2Status.Guid);

        var failRetry3PostMappingBuilder = wireMockAdminApi.GetMappingBuilder();
        failRetry3PostMappingBuilder.Given(m =>
            m.WithRequest(req => req.UsingPut().WithPath("/comparer/btms-decisions/" + mrn))
                .WithScenario(name)
                .WithWhenStateIs("Comparer Retry 2 Failure")
                .WithSetStateTo("Comparer Retry 3 Failure")
                .WithResponse(rsp => rsp.WithStatusCode(HttpStatusCode.ServiceUnavailable))
        );
        var postFailRetry3Status = await failRetry3PostMappingBuilder.BuildAndPostAsync();
        Assert.NotNull(postFailRetry3Status.Guid);

        var successfulPostMappingBuilder = wireMockAdminApi.GetMappingBuilder();
        successfulPostMappingBuilder.Given(m =>
            m.WithRequest(req => req.UsingPut().WithPath("/comparer/btms-decisions/" + mrn))
                .WithScenario(name)
                .WithWhenStateIs("Comparer Retry 3 Failure")
                .WithSetStateTo("Comparer Back Online")
                .WithResponse(rsp => rsp.WithStatusCode(HttpStatusCode.NoContent))
        );
        var postSuccessStatus = await successfulPostMappingBuilder.BuildAndPostAsync();
        Assert.NotNull(postSuccessStatus.Guid);
    }
}
