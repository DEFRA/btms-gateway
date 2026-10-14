using System.Net;
using System.Net.Http.Headers;
using System.Text;
using AwesomeAssertions;
using BtmsGateway.Config;

namespace BtmsGateway.Test.Config;

public class HttpClientConfigurableOptionsTests
{
    private sealed class TestHttpClientConfigurableOptions : HttpClientConfigurableOptions { }

    [Fact]
    public void When_configuring_client_with_https_Then_it_should_set_http2_and_base_address()
    {
        var options = new TestHttpClientConfigurableOptions { BaseAddress = "https://example.com" };
        using var client = new HttpClient();

        options.Configure(client);

        client.BaseAddress.Should().Be(new Uri("https://example.com"));
        client.DefaultRequestVersion.Should().Be(HttpVersion.Version20);
    }

    [Fact]
    public void When_configuring_client_with_http_Then_it_should_not_force_http2()
    {
        var options = new TestHttpClientConfigurableOptions { BaseAddress = "http://example.com" };
        using var client = new HttpClient();

        options.Configure(client);

        client.BaseAddress.Should().Be(new Uri("http://example.com"));
        client.DefaultRequestVersion.Should().NotBe(HttpVersion.Version20);
    }

    [Fact]
    public void When_username_and_password_are_provided_Then_basic_auth_header_should_be_set()
    {
        var options = new TestHttpClientConfigurableOptions
        {
            BaseAddress = "https://example.com",
            Username = "test-user",
            Password = "test-pass",
        };
        using var client = new HttpClient();

        options.Configure(client);

        var expectedToken = Convert.ToBase64String(Encoding.UTF8.GetBytes("test-user:test-pass"));

        options.BasicAuthCredential.Should().Be(expectedToken);
        client.DefaultRequestHeaders.Authorization.Should().Be(new AuthenticationHeaderValue("Basic", expectedToken));
    }

    [Fact]
    public void When_username_or_password_missing_Then_basic_auth_header_should_not_be_set()
    {
        var options = new TestHttpClientConfigurableOptions
        {
            BaseAddress = "https://example.com",
            Username = "test-user",
        };
        using var client = new HttpClient();

        options.Configure(client);

        options.BasicAuthCredential.Should().BeNull();
        client.DefaultRequestHeaders.Authorization.Should().BeNull();
    }
}
