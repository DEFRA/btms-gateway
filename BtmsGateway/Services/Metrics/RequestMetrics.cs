using System.Diagnostics;
using System.Diagnostics.Metrics;
using Amazon.CloudWatch.EMF.Model;

namespace BtmsGateway.Services.Metrics;

public interface IRequestMetrics
{
    void MessageReceived(string? messageType, string? requestPath, string? legend, string routeAction);
    void MessageSuccessfullySent(string? messageType, string? requestPath, string? legend, string routeAction);
    void RequestCompleted(string requestPath, string httpMethod, int statusCode, double milliseconds);
    void RequestFaulted(string requestPath, string httpMethod, int statusCode, Exception exception);
}

public class RequestMetrics : IRequestMetrics
{
    private readonly Counter<long> messagesReceived;
    private readonly Counter<long> messagesSuccessfullySent;
    private readonly Counter<long> requestsReceived;
    private readonly Counter<long> requestsFaulted;
    private readonly Histogram<double> requestDuration;

    public RequestMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MetricsConstants.MetricNames.MeterName);

        messagesReceived = meter.CreateCounter<long>(
            MetricsConstants.InstrumentNames.MessagesReceived,
            Unit.COUNT.ToString(),
            "Count of messages received"
        );

        messagesSuccessfullySent = meter.CreateCounter<long>(
            MetricsConstants.InstrumentNames.MessagesSuccessfullySent,
            Unit.COUNT.ToString(),
            "Count of messages successfully sent"
        );

        requestsReceived = meter.CreateCounter<long>(
            "RequestReceived",
            Unit.COUNT.ToString(),
            "Count of messages received"
        );

        requestDuration = meter.CreateHistogram<double>(
            "RequestDuration",
            Unit.MILLISECONDS.ToString(),
            "Duration of request"
        );

        requestsFaulted = meter.CreateCounter<long>("RequestFaulted", Unit.COUNT.ToString(), "Count of request faults");
    }

    public void MessageReceived(string? messageType, string? requestPath, string? legend, string routeAction)
    {
        messagesReceived.Add(1, BuildTags(messageType, requestPath, legend, routeAction));
    }

    public void MessageSuccessfullySent(string? messageType, string? requestPath, string? legend, string routeAction)
    {
        messagesSuccessfullySent.Add(1, BuildTags(messageType, requestPath, legend, routeAction));
    }

    public void RequestCompleted(string requestPath, string httpMethod, int statusCode, double milliseconds)
    {
        requestsReceived.Add(1, BuildRequestTags(requestPath, httpMethod, statusCode));
        requestDuration.Record(milliseconds, BuildRequestTags(requestPath, httpMethod, statusCode));
    }

    public void RequestFaulted(string requestPath, string httpMethod, int statusCode, Exception exception)
    {
        var tagList = BuildRequestTags(requestPath, httpMethod, statusCode);
        tagList.Add(MetricsConstants.RequestTags.ExceptionType, exception.GetType().Name);
        requestsFaulted.Add(1, tagList);
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

    private static TagList BuildRequestTags(string requestPath, string httpMethod, int statusCode)
    {
        return new TagList
        {
            { MetricsConstants.RequestTags.Service, Process.GetCurrentProcess().ProcessName },
            { MetricsConstants.RequestTags.RequestPath, requestPath },
            { MetricsConstants.RequestTags.HttpMethod, httpMethod },
            { MetricsConstants.RequestTags.StatusCode, statusCode },
        };
    }
}
