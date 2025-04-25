using System.Text;
using BtmsGateway.Config;
using FluentAssertions;

namespace BtmsGateway.Test.Config;

public class DecisionComparerApiOptionsTests
{
    [Fact]
    public void When_getting_basic_auth_credentials_and_username_and_password_exists_Then_base64_basic_auth_formatted_credentials_are_returned()
    {
        var decisionComparerApiOptions = new DecisionComparerApiOptions
        {
            BaseAddress = "https://some-uri",
            Username = "user",
            Password = "password"
        };

        var expectedResult = Convert.ToBase64String(Encoding.UTF8.GetBytes($"user:password"));

        decisionComparerApiOptions.BasicAuthCredential.Should().Be(expectedResult);
    }

    [Fact]
    public void When_getting_basic_auth_credentials_and_username_is_empty_Then_no_credentials_are_returned()
    {
        var decisionComparerApiOptions = new DecisionComparerApiOptions
        {
            BaseAddress = "https://some-uri",
            Username = null,
            Password = "password"
        };

        decisionComparerApiOptions.BasicAuthCredential.Should().BeNullOrEmpty();
    }

    [Fact]
    public void When_getting_basic_auth_credentials_and_password_is_empty_Then_no_credentials_are_returned()
    {
        var decisionComparerApiOptions = new DecisionComparerApiOptions
        {
            BaseAddress = "https://some-uri",
            Username = "user",
            Password = null
        };

        decisionComparerApiOptions.BasicAuthCredential.Should().BeNullOrEmpty();
    }
}