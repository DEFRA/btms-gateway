using System.Text.Json;
using System.Text.Json.Serialization;
using BtmsGateway.Services.Routing;
using BtmsGateway.Test.TestUtils;
using Microsoft.Extensions.DependencyInjection;

namespace BtmsGateway.Test.EndToEnd;

public abstract class TargetRoutingTestBase : IDisposable
{
    private bool _disposed;

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

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            var disposeTask = TestWebServer.DisposeAsync();
            if (disposeTask.IsCompleted)
            {
                return;
            }
            disposeTask.AsTask().GetAwaiter().GetResult();
        }

        _disposed = true;
    }
}