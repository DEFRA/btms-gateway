using System.Net.Http.Headers;
using BtmsGateway;
using BtmsGateway.Authentication;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Api.Tests.Endpoints;

public class EndpointTestBase : IClassFixture<TestWebApplicationFactory<Program>>
{
    private readonly TestWebApplicationFactory<Program> _factory;

    protected EndpointTestBase(TestWebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    protected virtual void ConfigureTestServices(IServiceCollection services) { }

    protected HttpClient CreateClient(bool addDefaultAuthorizationHeader = true, TestUser testUser = TestUser.Execute)
    {
        var builder = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(ConfigureTestServices);
        });

        var client = builder.CreateClient();

        if (addDefaultAuthorizationHeader)
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                BasicAuthenticationHandler.SchemeName,
                Convert.ToBase64String(
                    testUser switch
                    {
                        TestUser.ReadOnly => "IntegrationTests-Read:integration-tests-read"u8.ToArray(),
                        _ => "IntegrationTests-Execute:integration-tests-execute"u8.ToArray(),
                    }
                )
            );

        return client;
    }

    protected enum TestUser
    {
        ReadOnly,
        Execute,
    }
}
