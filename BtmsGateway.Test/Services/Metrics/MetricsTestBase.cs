using System.Diagnostics.Metrics;
using BtmsGateway.Services.Metrics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;

namespace BtmsGateway.Test.Services.Metrics;

public abstract class MetricsTestBase
{
    protected ServiceProvider ServiceProvider { get; }
    private IMeterFactory MeterFactory { get; }

    protected MetricsTestBase()
    {
        ServiceProvider = CreateServiceProvider();
        MeterFactory = ServiceProvider.GetRequiredService<IMeterFactory>();
    }

    private static ServiceProvider CreateServiceProvider()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddMetrics();
        serviceCollection.AddSingleton<IRequestMetrics, RequestMetrics>();
        serviceCollection.AddSingleton<IConsumerMetrics, ConsumerMetrics>();
        serviceCollection.AddSingleton<IHealthMetrics, HealthMetrics>();
        return serviceCollection.BuildServiceProvider();
    }

    protected MetricCollector<T> GetCollector<T>(string instrumentName)
        where T : struct
    {
        return new MetricCollector<T>(MeterFactory, MetricsConstants.MetricNames.MeterName, instrumentName);
    }
}
