using System.Text;
using BtmsGateway.Authentication;
using BtmsGateway.Config;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.WebEncoders.Testing;
using NSubstitute;

namespace BtmsGateway.Test.Authentication;

public class BasicAuthenticationHandlerTests
{
    private BasicAuthenticationHandler Subject { get; }
    private IOptionsMonitor<AuthenticationSchemeOptions> OptionsMonitor { get; } =
        Substitute.For<IOptionsMonitor<AuthenticationSchemeOptions>>();
    private AclOptions AclOptions { get; set; } = new();
    private Endpoint Endpoint = new Endpoint(null, null, null);

    public BasicAuthenticationHandlerTests()
    {
        Subject = new BasicAuthenticationHandler(
            OptionsMonitor,
            Substitute.For<ILoggerFactory>(),
            new UrlTestEncoder(),
            new OptionsWrapper<AclOptions>(AclOptions)
        );

        OptionsMonitor.Get("Basic").Returns(new AuthenticationSchemeOptions());
    }

    [Fact]
    public async Task WhenNoAuthorizationHeader_ShouldFail()
    {
        var context = new DefaultHttpContext();
        context.SetEndpoint(Endpoint);

        await Subject.InitializeAsync(Scheme(), context);

        await AuthenticateAndAssertFailure();
    }

    [Fact]
    public async Task WhenInvalidAuthorizationHeaderScheme_ShouldFail()
    {
        var context = new DefaultHttpContext { Request = { Headers = { Authorization = "InvalidScheme Value" } } };
        context.SetEndpoint(Endpoint);

        await Subject.InitializeAsync(Scheme(), context);

        await AuthenticateAndAssertFailure();
    }

    [Fact]
    public async Task WhenNoCredentials_ShouldFail()
    {
        var context = new DefaultHttpContext { Request = { Headers = { Authorization = "Basic " } } };
        context.SetEndpoint(Endpoint);

        await Subject.InitializeAsync(Scheme(), context);

        await AuthenticateAndAssertFailure();
    }

    [Theory]
    [InlineData(":secret")]
    [InlineData("username:")]
    public async Task WhenInvalidCredentials_ShouldFail(string credentials)
    {
        var context = new DefaultHttpContext
        {
            Request =
            {
                Headers = { Authorization = $"Basic {Convert.ToBase64String(Encoding.UTF8.GetBytes(credentials))}" },
            },
        };
        context.SetEndpoint(Endpoint);

        await Subject.InitializeAsync(Scheme(), context);

        await AuthenticateAndAssertFailure();
    }

    [Fact]
    public async Task WhenNoMatchingClientId_ShouldFail()
    {
        var context = new DefaultHttpContext
        {
            Request = { Headers = { Authorization = $"Basic {Convert.ToBase64String("client:secret"u8.ToArray())}" } },
        };
        context.SetEndpoint(Endpoint);

        AclOptions.Clients.Add("different-client", new AclOptions.Client { Secret = "secret", Scopes = [] });
        await Subject.InitializeAsync(Scheme(), context);

        await AuthenticateAndAssertFailure();
    }

    [Fact]
    public async Task WhenNoMatchingClientSecret_ShouldFail()
    {
        var context = new DefaultHttpContext
        {
            Request = { Headers = { Authorization = $"Basic {Convert.ToBase64String("client:secret"u8.ToArray())}" } },
        };
        context.SetEndpoint(Endpoint);

        AclOptions.Clients.Add("client", new AclOptions.Client { Secret = "different-secret", Scopes = [] });
        await Subject.InitializeAsync(Scheme(), context);

        await AuthenticateAndAssertFailure();
    }

    [Fact]
    public async Task WhenNoRequestEndpoint_ShouldHaveNoAuthResult()
    {
        var context = new DefaultHttpContext();

        await Subject.InitializeAsync(Scheme(), context);

        var result = await Subject.AuthenticateAsync();

        result.Failure.Should().BeNull();
        result.None.Should().BeTrue();
    }

    private static AuthenticationScheme Scheme()
    {
        return new AuthenticationScheme("Basic", "Basic", typeof(BasicAuthenticationHandler));
    }

    private async Task AuthenticateAndAssertFailure()
    {
        var result = await Subject.AuthenticateAsync();

        result.Failure.Should().NotBeNull();
        result.Failure.Should().BeOfType<AuthenticationFailureException>();
    }
}
