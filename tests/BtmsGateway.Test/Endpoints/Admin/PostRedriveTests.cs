using System.Net;
using BtmsGateway.Services.Admin;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit.Abstractions;

namespace BtmsGateway.Test.Endpoints.Admin;

public class PostRedriveTests(ApiWebApplicationFactory factory, ITestOutputHelper outputHelper)
    : EndpointTestBase(factory, outputHelper)
{
    private readonly IResourceEventsDeadLetterService _resourceEventsDeadLetterService =
        Substitute.For<IResourceEventsDeadLetterService>();

    protected override void ConfigureHostConfiguration(IConfigurationBuilder config)
    {
        base.ConfigureHostConfiguration(config);

        config.AddInMemoryCollection(new Dictionary<string, string> { ["AwsSqsOptions:AutoStartConsumers"] = "false" });
    }

    protected override void ConfigureTestServices(IServiceCollection services)
    {
        base.ConfigureTestServices(services);

        services.AddSingleton(_resourceEventsDeadLetterService);
    }

    [Fact]
    public async Task PostRedrive_When_unauthorized_Then_Unauthorized()
    {
        var client = CreateClient(false);

        var response = await client.PostAsync(Testing.Endpoints.AdminIntegration.PostRedrive(), null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PostRedrive_When_readonly_Then_Forbidden()
    {
        var client = CreateClient(testUser: TestUser.ReadOnly);

        var response = await client.PostAsync(Testing.Endpoints.AdminIntegration.PostRedrive(), null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task PostRedrive_When_authorized_redrive_fails_Then_InternalServerError()
    {
        var client = CreateClient();
        _resourceEventsDeadLetterService.Redrive(Arg.Any<CancellationToken>()).Returns(false);

        var response = await client.PostAsync(Testing.Endpoints.AdminIntegration.PostRedrive(), null);

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task PostRedrive_When_authorized_redrive_throws_exception_Then_InternalServerError()
    {
        var client = CreateClient();
        _resourceEventsDeadLetterService.Redrive(Arg.Any<CancellationToken>()).ThrowsAsync(new Exception("Test"));

        var response = await client.PostAsync(Testing.Endpoints.AdminIntegration.PostRedrive(), null);

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task PostRedrive_When_authorized_Then_Accepted()
    {
        var client = CreateClient();
        _resourceEventsDeadLetterService.Redrive(Arg.Any<CancellationToken>()).Returns(true);

        var response = await client.PostAsync(Testing.Endpoints.AdminIntegration.PostRedrive(), null);

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
    }
}
