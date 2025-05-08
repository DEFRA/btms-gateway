using System.Diagnostics;
using System.Diagnostics.Metrics;
using Amazon.CloudWatch.EMF.Model;

namespace BtmsGateway.Services.Metrics;

public interface IRequestMetrics
{
    void MessageReceived(string? messageType, string? requestPath, string? legend, string routeAction);
}

public class RequestMetrics : IRequestMetrics
{
    private readonly Counter<long> messagesReceived;

    public RequestMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MetricsConstants.MetricNames.MeterName);

        messagesReceived = meter.CreateCounter<long>(
            MetricsConstants.InstrumentNames.MessagesReceived,
            Unit.COUNT.ToString(),
            "Count of messages received"
        );
    }

    public void MessageReceived(string? messageType, string? requestPath, string? legend, string routeAction)
    {
        messagesReceived.Add(1, BuildTags(messageType, requestPath, legend, routeAction));
    }

    private static TagList BuildTags(string? messageType, string? requestPath, string? legend, string routeAction)
    {
        return new TagList
        {
            { MetricsConstants.RequestTags.Service, Process.GetCurrentProcess().ProcessName },
            { MetricsConstants.RequestTags.MessageType, messageType },
            { MetricsConstants.RequestTags.RequestPath, requestPath },
            { MetricsConstants.RequestTags.Legend, legend },
            { MetricsConstants.RequestTags.RouteAction, routeAction },
        };
    }
}
