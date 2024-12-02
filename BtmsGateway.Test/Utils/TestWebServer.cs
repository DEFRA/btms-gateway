using BtmsGateway.Config;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;

namespace BtmsGateway.Test.Utils;

public class TestWebServer : IAsyncDisposable
{
    private static int _portNumber = 5000;
    
    private readonly WebApplication _app;

    public TestHttpHandler OutboundTestHttpHandler { get; }
    public HttpClient HttpServiceClient { get; }
    public IServiceProvider Services { get; }

    public static TestWebServer BuildAndRun(params ServiceDescriptor[] testServices) => new(testServices);

    private TestWebServer(params ServiceDescriptor[] testServices)
    {
        OutboundTestHttpHandler = new TestHttpHandler();
        var url = $"http://localhost:{_portNumber}/";
        Interlocked.Increment(ref _portNumber);
        HttpServiceClient = new HttpClient { BaseAddress = new Uri(url) };

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls(url);
        builder.AddServices(Substitute.For<Serilog.ILogger>());
        foreach (var testService in testServices) builder.Services.Replace(testService);
        builder.ConfigureEndpoints();

        ConfigureWebApp.HttpProxyClientWithRetryBuilder?.AddHttpMessageHandler(() => OutboundTestHttpHandler);

        _app = builder.BuildWebApplication();
        Services = _app.Services;
        
        _app.RunAsync();
    }

    public async ValueTask DisposeAsync() => await _app.DisposeAsync();
}