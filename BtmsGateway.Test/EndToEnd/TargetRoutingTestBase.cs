using BtmsGateway.Test.TestUtils;

namespace BtmsGateway.Test.EndToEnd;

public abstract class TargetRoutingTestBase : IAsyncDisposable
{
    protected static readonly string FixturesPath = Path.Combine("EndToEnd", "Fixtures");

    protected readonly TestWebServer TestWebServer;
    protected readonly HttpClient HttpClient;

    protected TargetRoutingTestBase()
    {
        TestWebServer = TestWebServer.BuildAndRun();
        HttpClient = TestWebServer.HttpServiceClient;
    }
    
    public async ValueTask DisposeAsync() => await TestWebServer.DisposeAsync();
}