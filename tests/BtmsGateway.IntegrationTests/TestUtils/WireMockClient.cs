using RestEase;
using WireMock.Client;

namespace BtmsGateway.IntegrationTests.TestUtils;

public class WireMockClient
{
    public WireMockClient()
    {
        ResetWiremock().GetAwaiter().GetResult();
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
