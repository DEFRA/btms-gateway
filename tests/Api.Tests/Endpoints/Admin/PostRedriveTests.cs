using System.Net;
using BtmsGateway;
using BtmsGateway.Services.Admin;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Api.Tests.Endpoints.Admin;

public class PostRedriveTests(TestWebApplicationFactory<Program> factory) : EndpointTestBase(factory)
{
    private readonly ISqsService _sqsService = Substitute.For<ISqsService>();

    protected override void ConfigureTestServices(IServiceCollection services)
    {
        base.ConfigureTestServices(services);

        services.AddSingleton(_sqsService);
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
        _sqsService.Redrive(Arg.Any<CancellationToken>()).Returns(false);

        var response = await client.PostAsync(Testing.Endpoints.AdminIntegration.PostRedrive(), null);

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task PostRedrive_When_authorized_redrive_throws_exception_Then_InternalServerError()
    {
        var client = CreateClient();
        _sqsService.Redrive(Arg.Any<CancellationToken>()).ThrowsAsync(new Exception("Test"));

        var response = await client.PostAsync(Testing.Endpoints.AdminIntegration.PostRedrive(), null);

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task PostRedrive_When_authorized_Then_Accepted()
    {
        var client = CreateClient();
        _sqsService.Redrive(Arg.Any<CancellationToken>()).Returns(true);

        var response = await client.PostAsync(Testing.Endpoints.AdminIntegration.PostRedrive(), null);

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
    }
}
