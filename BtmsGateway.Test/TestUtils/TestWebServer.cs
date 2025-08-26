using Amazon.Extensions.NETCore.Setup;
using Amazon.Runtime;
using BtmsGateway.Config;
using BtmsGateway.Extensions;
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
using Serilog;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace BtmsGateway.Test.TestUtils;

public class TestWebServer : IDisposable
{
    private bool _disposed;

    private static int _portNumber = 5100;

    private readonly WebApplication _app;

    public TestHttpHandler RoutedHttpHandler { get; }
    public TestHttpHandler ForkedHttpHandler { get; }
    public TestHttpHandler ClientWithRetryHttpHandler { get; }
    public TestHttpHandler DecisionComparerClientWithRetryHttpHandler { get; }
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
        builder.Configuration.AddJsonFile(Path.Combine("EndToEnd", "Settings", "ConsumerSettings.json"));
        builder.ConfigureToType<RoutingConfig>();
        builder.ConfigureToType<HealthCheckConfig>();
        builder.WebHost.UseUrls(url);
        builder.AddServices(Substitute.For<ILogger>());
        foreach (var testService in testServices)
            builder.Services.Replace(testService);
        builder.Services.AddHealthChecks();
        builder.Services.AddOperationalMetrics();

        var options = builder.Configuration.GetAWSOptions();
        options.Credentials = new BasicAWSCredentials(
            builder.Configuration["AWS_ACCESS_KEY_ID"],
            builder.Configuration["AWS_SECRET_ACCESS_KEY"]
        );
        builder.Services.Replace(new ServiceDescriptor(typeof(AWSOptions), options));

        RoutedHttpHandler = new TestHttpHandler();
        ConfigureServices.HttpRoutedClientWithRetryBuilder?.AddHttpMessageHandler(() => RoutedHttpHandler);
        ForkedHttpHandler = new TestHttpHandler();
        ConfigureServices.HttpForkedClientWithRetryBuilder?.AddHttpMessageHandler(() => ForkedHttpHandler);
        ClientWithRetryHttpHandler = new TestHttpHandler();
        ConfigureServices.HttpClientWithRetryBuilder?.AddHttpMessageHandler(() => ClientWithRetryHttpHandler);
        DecisionComparerClientWithRetryHttpHandler = new TestHttpHandler();
        ConfigureServices.DecisionComparerHttpClientWithRetryBuilder?.ConfigureAdditionalHttpMessageHandlers(
            (handlers, _) => handlers.Insert(0, DecisionComparerClientWithRetryHttpHandler)
        );

        var app = builder.Build();

        app.UseMiddleware<MetricsMiddleware>();
        app.UseMiddleware<RoutingInterceptor>();

        app.MapHealthChecks("/health");

        app.UseCheckRoutesEndpoints();

        _app = app;

        _app.RunAsync();

        AsyncWaiter
            .WaitForAsync(async () =>
            {
                try
                {
                    var response = await HttpServiceClient.GetAsync("/health");
                    return response.IsSuccessStatusCode;
                }
                catch
                {
                    return false;
                }
            })
            .GetAwaiter()
            .GetResult();
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
            try
            {
                HttpServiceClient?.Dispose();
                RoutedHttpHandler?.Dispose();
                ForkedHttpHandler?.Dispose();
                ClientWithRetryHttpHandler?.Dispose();
                DecisionComparerClientWithRetryHttpHandler?.Dispose();
            }
            finally
            {
                try
                {
                    var disposeTask = _app.DisposeAsync();
                    if (!disposeTask.IsCompleted)
                    {
                        var waited = disposeTask.AsTask().Wait(TimeSpan.FromSeconds(10));
                        if (!waited)
#pragma warning disable CA2219
                            throw new TimeoutException("TestWebServer.DisposeAsync() timed out");
#pragma warning restore CA2219
                    }
                }
                catch (TaskCanceledException taskCanceledException)
                {
                    Console.WriteLine(
                        $"TaskCanceledException during dispose of TestWebServer: {taskCanceledException.Message}"
                    );
                }
                catch (AggregateException aggregateException)
                {
                    if (aggregateException.InnerExceptions.All(x => x is TaskCanceledException))
                    {
                        Console.WriteLine(
                            $"AggregateException of TaskCanceledException during dispose of TestWebServer: {aggregateException.Message}"
                        );
                    }
                    else
#pragma warning disable CA2219
                        throw;
#pragma warning restore CA2219
                }
            }
        }

        _disposed = true;
    }
}
