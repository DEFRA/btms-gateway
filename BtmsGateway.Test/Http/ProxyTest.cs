using BtmsGateway.Utils.Http;
using FluentAssertions;
using Serilog;
using Serilog.Core;

namespace BtmsGateway.Test.Http;

public class ProxyTest
{
    private readonly Logger _logger = new LoggerConfiguration().CreateLogger();

    private const string ProxyUri = "http://user:password@localhost:8080";
    private const string LocalProxy = "http://localhost:8080/";
    private const string Localhost = "http://localhost/";

    [Fact]
    public void ExtractProxyCredentials()
    {
        var proxy = new System.Net.WebProxy { BypassProxyOnLocal = true };

        Proxy.ConfigureProxy(proxy, ProxyUri);

        var credentials = proxy.Credentials?.GetCredential(new Uri(ProxyUri), "Basic");

        credentials?.UserName.Should().Be("user");
        credentials?.Password.Should().Be("password");
    }

    [Fact]
    public void ExtractProxyEmptyCredentials()
    {
        var noPasswordUri = "http://user@localhost:8080";

        var proxy = new System.Net.WebProxy { BypassProxyOnLocal = true };

        Proxy.ConfigureProxy(proxy, noPasswordUri);

        proxy.Credentials.Should().BeNull();
    }

    [Fact]
    public void ExtractProxyUri()
    {
        var proxy = new System.Net.WebProxy { BypassProxyOnLocal = true };

        Proxy.ConfigureProxy(proxy, ProxyUri);
        proxy.Address.Should().NotBeNull();
        proxy.Address?.AbsoluteUri.Should().Be(LocalProxy);
    }

    [Fact]
    public void CreateProxyFromUri()
    {
        var proxy = Proxy.CreateProxy(ProxyUri);

        proxy.Address.Should().NotBeNull();
        proxy.Address?.AbsoluteUri.Should().Be(LocalProxy);
    }

    [Fact]
    public void CreateNoProxyFromEmptyUri()
    {
        var proxy = Proxy.CreateProxy(null);

        proxy.Address.Should().BeNull();
    }

    [Fact]
    public void ProxyShouldBypassLocal()
    {
        var proxy = Proxy.CreateProxy(ProxyUri);

        proxy.BypassProxyOnLocal.Should().BeTrue();
        proxy.IsBypassed(new Uri(Localhost)).Should().BeTrue();
        proxy.IsBypassed(new Uri("https://defra.gov.uk")).Should().BeFalse();
    }

    [Fact]
    public void HandlerShouldHaveProxy()
    {
        var handler = Proxy.CreateHttpClientHandler(ProxyUri);

        handler.Proxy.Should().NotBeNull();
        handler.UseProxy.Should().BeTrue();
        handler.Proxy?.Credentials.Should().NotBeNull();
        handler.Proxy?.GetProxy(new Uri(Localhost)).Should().NotBeNull();
        handler.Proxy?.GetProxy(new Uri("http://google.com")).Should().NotBeNull();
        handler.Proxy?.GetProxy(new Uri(Localhost))?.AbsoluteUri.Should().Be(Localhost);
        handler.Proxy?.GetProxy(new Uri("http://google.com"))?.AbsoluteUri.Should().Be(LocalProxy);
    }
}
