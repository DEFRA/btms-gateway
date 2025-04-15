using System.Text.Json;
using System.Text.Json.Serialization;
using BtmsGateway.Services.Routing;
using BtmsGateway.Test.TestUtils;
using Microsoft.Extensions.DependencyInjection;

namespace BtmsGateway.Test.EndToEnd;

public abstract class TargetRoutingTestBase : IDisposable
{
    protected static readonly string FixturesPath = Path.Combine("EndToEnd", "Fixtures");

    private static readonly JsonSerializerOptions JsonSerializerOptions = new() { Converters = { new JsonStringEnumConverter() } };

    protected readonly TestWebServer TestWebServer;
    protected readonly HttpClient HttpClient;
    protected List<ServiceDescriptor> Services = new();

    protected TargetRoutingTestBase()
    {
        var routingConfigJson = File.ReadAllText(Path.Combine(FixturesPath, "TargetRoutingConfig.json"));
        Services.Add(ServiceDescriptor.Singleton(JsonSerializer.Deserialize<RoutingConfig>(routingConfigJson, JsonSerializerOptions)));
        TestWebServer = TestWebServer.BuildAndRun(Services.ToArray());
        HttpClient = TestWebServer.HttpServiceClient;
    }

    public void Dispose() => TestWebServer.DisposeAsync().GetAwaiter().GetResult();
}