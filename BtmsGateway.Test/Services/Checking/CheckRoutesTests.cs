using System.Net;
using BtmsGateway.Services.Checking;
using BtmsGateway.Test.TestUtils;
using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Serilog;

namespace BtmsGateway.Test.Services.Checking;

public class CheckRoutesTests
{
    private readonly TestHttpHandler _httpHandler = new TestHttpHandler();
    private readonly IHttpClientFactory _httpClientFactory = Substitute.For<IHttpClientFactory>();
    private readonly ILogger _logger = Substitute.For<ILogger>();
    private readonly IProcessRunner _processRunner = Substitute.For<IProcessRunner>();

    private readonly CheckRoutes _checkRoutes;

    public CheckRoutesTests()
    {
        var healthCheckConfig = new HealthCheckConfig
        {
            Disabled = false,
            AutomatedHealthCheckDisabled = false,
            Urls = new Dictionary<string, HealthCheckUrl>
            {
                { "Test", new HealthCheckUrl
                    {
                        Disabled = false,
                        Method = "GET",
                        Url = "http://test",
                        HostHeader = "test",
                        IncludeInAutomatedHealthCheck = true
                    }
                },
                { "IPAFFS Test", new HealthCheckUrl
                    {
                        Disabled = false,
                        Method = "GET",
                        Url = "http://test-ipaffs",
                        HostHeader = "test",
                        IncludeInAutomatedHealthCheck = true
                    }
                }
            }
        };

        var httpClient = new HttpClient(_httpHandler);

        _httpClientFactory.CreateClient(Arg.Any<string>()).Returns(httpClient);

        _checkRoutes = new CheckRoutes(healthCheckConfig, _httpClientFactory, _logger, _processRunner);
    }

    [Fact]
    public async Task When_checking_all_routes_Then_should_perform_http_nslookup_and_dig_checks()
    {
        _httpHandler.SetNextResponse("route-content", () => HttpStatusCode.OK);
        _processRunner.RunProcess(Arg.Any<string>(), Arg.Any<String>()).Returns("OK");

        var result = await _checkRoutes.CheckAll();

        result.Count().Should().Be(6);
        result.Should().ContainEquivalentOf(new { CheckType = "HTTP", RouteUrl = "GET http://test", ResponseResult = "OK (200)" });
        result.Should().ContainEquivalentOf(new { CheckType = "nslookup", RouteUrl = "test", ResponseResult = "OK" });
        result.Should().ContainEquivalentOf(new { CheckType = "dig", RouteUrl = "test", ResponseResult = "OK" });
        result.Should().ContainEquivalentOf(new { CheckType = "HTTP", RouteUrl = "GET http://test-ipaffs", ResponseResult = "OK (200)" });
        result.Should().ContainEquivalentOf(new { CheckType = "nslookup", RouteUrl = "test-ipaffs", ResponseResult = "OK" });
        result.Should().ContainEquivalentOf(new { CheckType = "dig", RouteUrl = "test-ipaffs", ResponseResult = "OK" });
    }

    [Fact]
    public async Task When_checking_ipaffs_routes_Then_should_perform_http_nslookup_and_dig_checks()
    {
        _httpHandler.SetNextResponse("route-content", () => HttpStatusCode.OK);
        _processRunner.RunProcess(Arg.Any<string>(), Arg.Any<String>()).Returns("OK");

        var result = await _checkRoutes.CheckIpaffs();

        result.Count().Should().Be(3);
        result.Should().ContainEquivalentOf(new { CheckType = "HTTP", RouteUrl = "GET http://test-ipaffs", ResponseResult = "OK (200)" });
        result.Should().ContainEquivalentOf(new { CheckType = "nslookup", RouteUrl = "test-ipaffs", ResponseResult = "OK" });
        result.Should().ContainEquivalentOf(new { CheckType = "dig", RouteUrl = "test-ipaffs", ResponseResult = "OK" });
    }

    [Fact]
    public async Task When_checking_routes_and_exceptions_occur_Then_results_should_contain_exception_message()
    {
        _httpHandler.SetNextResponse(exceptionToThrow: new Exception("Test Http exception message"));
        _processRunner.RunProcess(Arg.Any<string>(), Arg.Any<String>()).ThrowsAsync(new Exception("Test Network exception message"));

        var result = await _checkRoutes.CheckAll();

        result.Count().Should().Be(6);
        result.Should().ContainEquivalentOf(new { CheckType = "HTTP", RouteUrl = "GET http://test", ResponseResult = "\"Test Http exception message\" " });
        result.Should().ContainEquivalentOf(new { CheckType = "nslookup", RouteUrl = "test", ResponseResult = "\"One or more errors occurred. (Test Network exception message)\" \"Test Network exception message\"" });
        result.Should().ContainEquivalentOf(new { CheckType = "dig", RouteUrl = "test", ResponseResult = "\"One or more errors occurred. (Test Network exception message)\" \"Test Network exception message\"" });
        result.Should().ContainEquivalentOf(new { CheckType = "HTTP", RouteUrl = "GET http://test-ipaffs", ResponseResult = "\"Test Http exception message\" " });
        result.Should().ContainEquivalentOf(new { CheckType = "nslookup", RouteUrl = "test-ipaffs", ResponseResult = "\"One or more errors occurred. (Test Network exception message)\" \"Test Network exception message\"" });
        result.Should().ContainEquivalentOf(new { CheckType = "dig", RouteUrl = "test-ipaffs", ResponseResult = "\"One or more errors occurred. (Test Network exception message)\" \"Test Network exception message\"" });
    }
}