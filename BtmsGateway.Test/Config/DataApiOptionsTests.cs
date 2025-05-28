using System.Net.Http.Headers;
using System.Text;
using BtmsGateway.Config;
using FluentAssertions;

namespace BtmsGateway.Test.Config;

public class DataApiOptionsTests
{
    [Fact]
    public void When_getting_basic_auth_credentials_and_username_and_password_exists_Then_base64_basic_auth_formatted_credentials_are_returned()
    {
        var dataApiOptions = new DataApiOptions
        {
            BaseAddress = "https://some-uri",
            Username = "user",
            Password = "password",
        };

        var expectedResult = Convert.ToBase64String(Encoding.UTF8.GetBytes($"user:password"));

        dataApiOptions.BaseAddress.Should().Be($"https://some-uri");
        dataApiOptions.BasicAuthCredential.Should().Be(expectedResult);
    }

    [Fact]
    public void When_getting_basic_auth_credentials_and_username_is_empty_Then_no_credentials_are_returned()
    {
        var dataApiOptions = new DataApiOptions
        {
            BaseAddress = "https://some-uri",
            Username = null,
            Password = "password",
        };

        dataApiOptions.BasicAuthCredential.Should().BeNullOrEmpty();
    }

    [Fact]
    public void When_getting_basic_auth_credentials_and_password_is_empty_Then_no_credentials_are_returned()
    {
        var dataApiOptions = new DataApiOptions
        {
            BaseAddress = "https://some-uri",
            Username = "user",
            Password = null,
        };

        dataApiOptions.BasicAuthCredential.Should().BeNullOrEmpty();
    }

    [Fact]
    public void When_configure_http_client_Then_client_should_be_configured_correctly()
    {
        var dataApiOptions = new DataApiOptions
        {
            BaseAddress = "https://some-uri",
            Username = "user",
            Password = "password",
        };
        var httpClient = new HttpClient();

        dataApiOptions.Configure(httpClient);

        httpClient.BaseAddress.Should().Be($"https://some-uri");
        httpClient
            .DefaultRequestHeaders.Authorization.Should()
            .BeEquivalentTo(new AuthenticationHeaderValue("Basic", dataApiOptions.BasicAuthCredential));
        httpClient.DefaultRequestVersion.Should().BeEquivalentTo(new Version(2, 0));
    }
}
