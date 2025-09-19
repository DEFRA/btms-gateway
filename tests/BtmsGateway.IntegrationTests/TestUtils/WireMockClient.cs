using RestEase;
using WireMock.Client;

namespace BtmsGateway.IntegrationTests.TestUtils;

public class WireMockClient
{
    public WireMockClient()
    {
        WireMockAdminApi.ResetMappingsAsync().GetAwaiter().GetResult();
        WireMockAdminApi.ResetRequestsAsync().GetAwaiter().GetResult();
        WireMockAdminApi.ReloadStaticMappingsAsync().GetAwaiter().GetResult();
    }

    public IWireMockAdminApi WireMockAdminApi { get; } = RestClient.For<IWireMockAdminApi>("http://localhost:9090");

    public async Task ResetWiremock()
    {
        await WireMockAdminApi.ResetMappingsAsync();
        await WireMockAdminApi.ResetRequestsAsync();
        await WireMockAdminApi.ReloadStaticMappingsAsync();
    }
}

[CollectionDefinition("UsesWireMockClient")]
public class WireMockClientCollection : ICollectionFixture<WireMockClient>;
