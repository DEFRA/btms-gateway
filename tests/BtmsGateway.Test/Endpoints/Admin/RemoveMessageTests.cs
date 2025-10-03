using System.Net;
using BtmsGateway.Services.Admin;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit.Abstractions;

namespace BtmsGateway.Test.Endpoints.Admin;

public class RemoveMessageTests(ApiWebApplicationFactory factory, ITestOutputHelper outputHelper)
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

        var response = await client.PostAsync(Testing.Endpoints.Redrive.DeadLetterQueue.RemoveMessage(), null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task When_readonly_Then_Forbidden()
    {
        var client = CreateClient(testUser: TestUser.ReadOnly);

        var response = await client.PostAsync(Testing.Endpoints.Redrive.DeadLetterQueue.RemoveMessage(), null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task When_authorized_remove_message_returns_Then_OK()
    {
        var client = CreateClient();
        const string messageId = "messageId";
        _resourceEventsDeadLetterService.Remove(messageId, Arg.Any<CancellationToken>()).Returns("Result from Remove");

        var response = await client.PostAsync(Testing.Endpoints.Redrive.DeadLetterQueue.RemoveMessage(messageId), null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).Should().Be("Result from Remove");
    }

    [Fact]
    public async Task When_authorized_remove_message_throws_exception_Then_InternalServerError()
    {
        var client = CreateClient();
        const string messageId = "messageId";
        _resourceEventsDeadLetterService
            .Remove(messageId, Arg.Any<CancellationToken>())
            .ThrowsAsync(new Exception("Test"));

        var response = await client.PostAsync(Testing.Endpoints.Redrive.DeadLetterQueue.RemoveMessage(messageId), null);

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    }
}
