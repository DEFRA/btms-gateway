namespace BtmsGateway.Domain;

public static class MessagingConstants
{
    public static class MessageAttributeKeys
    {
        public const string CorrelationId = "CorrelationId";
        public const string MessageType = "messageType";
    }

    public static class MessageTypes
    {
        public const string ClearanceRequest = "ClearanceRequest";
        public const string Finalisation = "Finalisation";
        public const string InboundError = "InboundError";
    }

    public static class SoapMessageTypes
    {
        public const string ALVSClearanceRequest = "ALVSClearanceRequest";
        public const string FinalisationNotificationRequest = "FinalisationNotificationRequest";
        public const string ALVSErrorNotificationRequest = "ALVSErrorNotificationRequest";
    }
}