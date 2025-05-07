namespace BtmsGateway.Services.Metrics;

public static class MetricsConstants
{
    public static class MetricNames
    {
        public const string MeterName = "Btms.Gateway";
    }

    public static class RequestTags
    {
        public const string MessageType = "MessageType";
        public const string RequestPath = "RequestPath";
        public const string Legend = "Legend";
        public const string RouteAction = "RouteAction";
    }
    
    public static class ConsumerTags
    {
        public const string QueueName = "QueueName";
        public const string ConsumerType = "ConsumerType";
        public const string Service = "ServiceName";
        public const string ExceptionType = "ExceptionType";
        public const string ResourceType = "ResourceType";
        public const string SubResourceType = "SubResourceType";
    }
}