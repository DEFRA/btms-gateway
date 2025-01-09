using BtmsGateway.Config;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;

namespace BtmsGateway.Test.TestUtils;

public class TestWebServer : IAsyncDisposable
{
    private static int _portNumber = 5100;
    
    private readonly WebApplication _app;

    public TestHttpHandler RoutedHttpHandler { get; }
    public TestHttpHandler ForkedHttpHandler { get; }
    public HttpClient HttpServiceClient { get; }
    public IServiceProvider Services => _app.Services;

    public static TestWebServer BuildAndRun(params ServiceDescriptor[] testServices) => new(testServices);

    private TestWebServer(params ServiceDescriptor[] testServices)
    {
        var url = $"http://localhost:{_portNumber}/";
        Interlocked.Increment(ref _portNumber);
        HttpServiceClient = new HttpClient { BaseAddress = new Uri(url) };

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls(url);
        builder.AddServices(Substitute.For<Serilog.ILogger>());
        foreach (var testService in testServices) builder.Services.Replace(testService);
        builder.ConfigureEndpoints();

        RoutedHttpHandler = new TestHttpHandler();
        ConfigureWebApp.HttpRoutedClientWithRetryBuilder?.AddHttpMessageHandler(() => RoutedHttpHandler);
        ForkedHttpHandler = new TestHttpHandler();
        ConfigureWebApp.HttpForkedClientWithRetryBuilder?.AddHttpMessageHandler(() => ForkedHttpHandler);

        _app = builder.BuildWebApplication();
        
        _app.RunAsync();
    }

    public async ValueTask DisposeAsync() => await _app.DisposeAsync();
}