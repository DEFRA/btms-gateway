using System.Net.Http.Headers;

namespace BtmsGateway.IntegrationTests.TestBase;

[Trait("Category", "IntegrationTest")]
[Collection("Integration Tests")]
public abstract class IntegrationTestBase
{
    protected static HttpClient CreateHttpClient(bool withAuthentication = true)
    {
        var httpClient = new HttpClient { BaseAddress = new Uri("http://localhost:3091") };

        if (withAuthentication)
        {
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                "Basic",
                // See compose.yml for username, password and scope configuration
                Convert.ToBase64String("IntegrationTests:integration-tests-pwd"u8.ToArray())
            );
        }

        return httpClient;
    }
}
