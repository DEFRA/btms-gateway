using System.Net;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Xunit.Abstractions;

namespace BtmsGateway.Test.Endpoints.Admin;

public class CountTests(ApiWebApplicationFactory factory, ITestOutputHelper outputHelper)
    : NoConsumersTestBase(factory, outputHelper)
{
    private readonly IResourceEventsDeadLetterService _resourceEventsDeadLetterService =
        Substitute.For<IResourceEventsDeadLetterService>();

    protected override void ConfigureTestServices(IServiceCollection services)
    {
        base.ConfigureTestServices(services);

        services.AddSingleton(_resourceEventsDeadLetterService);
    }

    [Fact]
    public async Task When_unauthorized_Then_Unauthorized()
    {
        var client = CreateClient(false);

        var response = await client.GetAsync(Testing.Endpoints.Redrive.DeadLetterQueue.Count());

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task When_readonly_Then_Forbidden()
    {
        var client = CreateClient(testUser: TestUser.ReadOnly);

        var response = await client.GetAsync(Testing.Endpoints.Redrive.DeadLetterQueue.Count());

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task When_authorized_and_count_returns_Then_OK()
    {
        var client = CreateClient();
        _resourceEventsDeadLetterService.GetCount(Arg.Any<CancellationToken>()).Returns(Task.FromResult(1));

        var response = await client.GetAsync(Testing.Endpoints.Redrive.DeadLetterQueue.Count());

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task When_authorized_and_count_throws_exception_Then_InternalServerError()
    {
        var client = CreateClient();
        _resourceEventsDeadLetterService.GetCount(Arg.Any<CancellationToken>()).ThrowsAsync(new Exception("Test"));

        var response = await client.GetAsync(Testing.Endpoints.Redrive.DeadLetterQueue.Count());

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    }
}
