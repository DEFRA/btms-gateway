using Amazon.Extensions.NETCore.Setup;
using Amazon.Runtime;
using BtmsGateway.Config;
using BtmsGateway.Middleware;
using BtmsGateway.Services.Checking;
using BtmsGateway.Services.Routing;
using BtmsGateway.Utils;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

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
        builder.Configuration.AddJsonFile(Path.Combine("EndToEnd", "Settings", "localstack.json"));
        builder.ConfigureToType<RoutingConfig>();
        builder.ConfigureToType<HealthCheckConfig>();
        builder.WebHost.UseUrls(url);
        builder.AddServices(Substitute.For<Serilog.ILogger>());
        foreach (var testService in testServices) builder.Services.Replace(testService);
        builder.Services.AddHealthChecks();
        
        var options = builder.Configuration.GetAWSOptions();
        options.Credentials = new BasicAWSCredentials(builder.Configuration["AWS_ACCESS_KEY_ID"], builder.Configuration["AWS_SECRET_ACCESS_KEY"]);
        builder.Services.Replace(new ServiceDescriptor(typeof(AWSOptions), options));
        
        RoutedHttpHandler = new TestHttpHandler();
        ConfigureServices.HttpRoutedClientWithRetryBuilder?.AddHttpMessageHandler(() => RoutedHttpHandler);
        ForkedHttpHandler = new TestHttpHandler();
        ConfigureServices.HttpForkedClientWithRetryBuilder?.AddHttpMessageHandler(() => ForkedHttpHandler);

        var app = builder.Build();

        app.UseMiddleware<RoutingInterceptor>();

        app.MapHealthChecks("/health");

        app.UseCheckRoutesEndpoints();

        _app = app;

        _app.RunAsync();
    }

    public async ValueTask DisposeAsync() => await _app.DisposeAsync();
}