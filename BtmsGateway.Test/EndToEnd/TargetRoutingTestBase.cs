using System.Text.Json;
using System.Text.Json.Serialization;
using BtmsGateway.Services.Routing;
using BtmsGateway.Test.TestUtils;
using Microsoft.Extensions.DependencyInjection;

namespace BtmsGateway.Test.EndToEnd;

public abstract class TargetRoutingTestBase : IAsyncDisposable
{
    protected static readonly string FixturesPath = Path.Combine("EndToEnd", "Fixtures");
    
    private static readonly JsonSerializerOptions JsonSerializerOptions = new() { Converters = { new JsonStringEnumConverter() } };

    protected readonly TestWebServer TestWebServer;
    protected readonly HttpClient HttpClient;

    protected TargetRoutingTestBase()
    {
        var routingConfigJson = File.ReadAllText(Path.Combine(FixturesPath, "TargetRoutingConfig.json"));
        TestWebServer = TestWebServer.BuildAndRun(ServiceDescriptor.Singleton(JsonSerializer.Deserialize<RoutingConfig>(routingConfigJson, JsonSerializerOptions)));
        HttpClient = TestWebServer.HttpServiceClient;
    }
    
    public async ValueTask DisposeAsync() => await TestWebServer.DisposeAsync();
}