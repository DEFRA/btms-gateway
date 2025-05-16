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
        public const string InstanceId = "InstanceId";
    }

    public static class InstrumentNames
    {
        public const string MessagesReceived = "MessagesReceived";
        public const string MessagingConsume = "MessagingConsume";
        public const string MessagingConsumeErrors = "MessagingConsumeErrors";
        public const string MessagingConsumeActive = "MessagingConsumeActive";
        public const string MessagingConsumeDuration = "MessagingConsumeDuration";
        public const string Health = "Health";
    }
}
