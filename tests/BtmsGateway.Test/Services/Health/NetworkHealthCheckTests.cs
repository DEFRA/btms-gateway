using System.Net;
using BtmsGateway.Services.Checking;
using BtmsGateway.Services.Health;
using BtmsGateway.Test.TestUtils;
using BtmsGateway.Utils.Http;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging.Abstractions;

namespace BtmsGateway.Test.Services.Health;

public class NetworkHealthCheckTests
{
    [Theory]
    [InlineData(HttpStatusCode.OK, HealthStatus.Healthy)]
    [InlineData(HttpStatusCode.Accepted, HealthStatus.Healthy)]
    [InlineData(HttpStatusCode.NoContent, HealthStatus.Healthy)]
    [InlineData(HttpStatusCode.NotFound, HealthStatus.Degraded)]
    [InlineData(HttpStatusCode.BadRequest, HealthStatus.Degraded)]
    [InlineData(HttpStatusCode.BadGateway, HealthStatus.Degraded)]
    [InlineData(HttpStatusCode.ServiceUnavailable, HealthStatus.Degraded)]
    [InlineData(HttpStatusCode.MethodNotAllowed, HealthStatus.Degraded)]
    public async Task When_health_checking_a_route_Then_should_set_correct_status(
        HttpStatusCode statusCode,
        HealthStatus healthStatus
    )
    {
        var testHttpHandler = new TestHttpHandler();
        testHttpHandler.SetNextResponse("route-content", () => statusCode);
        var healthCheckUrl = new HealthCheckUrl
        {
            Method = "GET",
            Url = "http://1.2.3.4/path",
            HostHeader = "localhost",
            IncludeInAutomatedHealthCheck = true,
            Disabled = false,
        };
        var routeHealthCheck = GetRouteHealthCheck(healthCheckUrl, testHttpHandler);

        var result = await routeHealthCheck.CheckHealthAsync(new HealthCheckContext());

        result.Status.Should().Be(healthStatus);
        result.Data["status"].Should().Be($"{(int)statusCode} {statusCode}");
    }

    [Fact]
    public async Task When_health_checking_a_route_Then_should_populate_other_information()
    {
        var testHttpHandler = new TestHttpHandler();
        testHttpHandler.SetNextResponse("route-content", () => HttpStatusCode.OK);
        var healthCheckUrl = new HealthCheckUrl
        {
            Method = "GET",
            Url = "http://1.2.3.4/path",
            HostHeader = "localhost",
            IncludeInAutomatedHealthCheck = true,
            Disabled = false,
        };
        var routeHealthCheck = GetRouteHealthCheck(healthCheckUrl, testHttpHandler);

        var result = await routeHealthCheck.CheckHealthAsync(new HealthCheckContext());

        result.Description.Should().Be("Network route: Health Check Name");
        result.Exception.Should().BeNull();
        result.Data["route"].Should().Be(healthCheckUrl.Url);
        result.Data["host"].Should().Be(healthCheckUrl.HostHeader);
        result.Data["method"].Should().Be(healthCheckUrl.Method);
        result.Data["content"].Should().Be("route-content");
        result.Data.Keys.Should().NotContain("error");
    }

    [Fact]
    public async Task When_health_checking_a_route_that_throws_Then_should_set_correct_status_and_populate_other_information()
    {
        var testHttpHandler = new TestHttpHandler();
        var exceptionToThrow = new ApplicationException(
            "Error message",
            new ArithmeticException("Inner error message")
        );
        testHttpHandler.SetNextResponse(exceptionToThrow: exceptionToThrow);
        var healthCheckUrl = new HealthCheckUrl
        {
            Method = "GET",
            Url = "http://1.2.3.4/path",
            HostHeader = "localhost",
            IncludeInAutomatedHealthCheck = true,
            Disabled = false,
        };
        var routeHealthCheck = GetRouteHealthCheck(healthCheckUrl, testHttpHandler);

        var result = await routeHealthCheck.CheckHealthAsync(new HealthCheckContext());

        result.Status.Should().Be(HealthStatus.Degraded);
        result.Description.Should().Be("Network route: Health Check Name");
        result.Exception.Should().Be(exceptionToThrow);
        result.Data["route"].Should().Be(healthCheckUrl.Url);
        result.Data["host"].Should().Be(healthCheckUrl.HostHeader);
        result.Data["method"].Should().Be(healthCheckUrl.Method);
        result.Data["status"].Should().Be("");
        result.Data["content"].Should().Be("");
        result.Data["error"].Should().Be("Error message - Inner error message");
    }

    [Fact]
    public async Task When_health_checking_a_route_that_throws_timeout_Then_should_set_correct_status_and_populate_other_information()
    {
        var testHttpHandler = new TestHttpHandler();
        var exceptionToThrow = new TaskCanceledException("Error message", new TimeoutException("Inner error message"));
        testHttpHandler.SetNextResponse(exceptionToThrow: exceptionToThrow);
        var healthCheckUrl = new HealthCheckUrl
        {
            Method = "GET",
            Url = "http://1.2.3.4/path",
            HostHeader = "localhost",
            IncludeInAutomatedHealthCheck = true,
            Disabled = false,
        };
        var routeHealthCheck = GetRouteHealthCheck(healthCheckUrl, testHttpHandler);

        var result = await routeHealthCheck.CheckHealthAsync(new HealthCheckContext());

        result.Status.Should().Be(HealthStatus.Degraded);
        result.Description.Should().Be("Network route: Health Check Name");
        result.Data["route"].Should().Be(healthCheckUrl.Url);
        result.Data["host"].Should().Be(healthCheckUrl.HostHeader);
        result.Data["method"].Should().Be(healthCheckUrl.Method);
        result.Data["status"].Should().Be("");
        result.Data["content"].Should().Be("");
        result
            .Data["error"]
            .Should()
            .Be("The network check has cancelled, probably because it timed out after 15 seconds - Error message");
    }

    [Fact]
    public async Task When_health_checking_a_route_that_should_consider_additional_status_as_successful_Then_should_set_correct_status()
    {
        var testHttpHandler = new TestHttpHandler();
        testHttpHandler.SetNextResponse("route-content", () => HttpStatusCode.MethodNotAllowed);
        var healthCheckUrl = new HealthCheckUrl
        {
            Method = "GET",
            Url = "http://1.2.3.4/path",
            HostHeader = "localhost",
            IncludeInAutomatedHealthCheck = true,
            Disabled = false,
            AdditionalSuccessStatuses = [405],
        };
        var routeHealthCheck = GetRouteHealthCheck(healthCheckUrl, testHttpHandler);

        var result = await routeHealthCheck.CheckHealthAsync(new HealthCheckContext());

        result.Status.Should().Be(HealthStatus.Healthy);
        result.Data["status"].Should().Be($"{(int)HttpStatusCode.MethodNotAllowed} {HttpStatusCode.MethodNotAllowed}");
    }

    private static NetworkHealthCheck GetRouteHealthCheck(
        HealthCheckUrl healthCheckUrl,
        TestHttpHandler testHttpHandler
    )
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddHttpClient(Proxy.RoutedClientWithRetry).AddHttpMessageHandler(() => testHttpHandler);
        serviceCollection.AddSingleton(s => new NetworkHealthCheck(
            "Health_Check_Name",
            healthCheckUrl,
            s.GetRequiredService<IHttpClientFactory>(),
            NullLogger<NetworkHealthCheck>.Instance
        ));
        var services = serviceCollection.BuildServiceProvider();
        return services.GetRequiredService<NetworkHealthCheck>();
    }
}
