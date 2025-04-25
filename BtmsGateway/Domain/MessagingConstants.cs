namespace BtmsGateway.Domain;

public static class MessagingConstants
{
    public static class MessageAttributeKeys
    {
        public const string CorrelationId = "CorrelationId";
        public const string InboundHmrcMessageType = "InboundHmrcMessageType";
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

    public enum DecisionSource
    {
        Alvs,
        Btms
    }

    public static class Destinations
    {
        public const string BtmsCds = "BtmsCds";
        public const string BtmsDecisionComparer = "BtmsDecisionComparer";
        public const string AlvsDecisionComparer = "AlvsDecisionComparer";
    }
}