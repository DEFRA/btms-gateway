namespace BtmsGateway.Services.Metrics;

public static class MetricsConstants
{
    public static class MetricNames
    {
        public const string MeterName = "Btms.Gateway";
    }

    public static class RequestTags
    {
        public const string Service = "ServiceName";
        public const string MessageType = "MessageType";
        public const string RequestPath = "RequestPath";
        public const string Legend = "Legend";
        public const string RouteAction = "RouteAction";
        public const string HttpMethod = "HttpMethod";
        public const string StatusCode = "StatusCode";
        public const string ExceptionType = "ExceptionType";
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

    public static class HealthTags
    {
        public const string Service = "ServiceName";
        public const string Component = "Component";
        public const string Description = "Description";
    }

    public static class InstrumentNames
    {
        public const string MessagesReceived = "MessagesReceived";
        public const string MessagesSuccessfullySent = "MessagesSuccessfullySent";
        public const string RequestReceived = "RequestReceived";
        public const string RequestDuration = "RequestDuration";
        public const string RequestFaulted = "RequestFaulted";
        public const string MessagingConsume = "MessagingConsume";
        public const string MessagingConsumeErrors = "MessagingConsumeErrors";
        public const string MessagingConsumeActive = "MessagingConsumeActive";
        public const string MessagingConsumeDuration = "MessagingConsumeDuration";
        public const string Health = "Health";
    }
}
