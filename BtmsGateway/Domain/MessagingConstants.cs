namespace BtmsGateway.Domain;

public static class MessagingConstants
{
    public static class MessageAttributeKeys
    {
        public const string CorrelationId = "CorrelationId";
        public const string InboundHmrcMessageType = "InboundHmrcMessageType";
        public const string ResourceId = "ResourceId";
    }

    public static class MessageTypes
    {
        public const string ClearanceRequest = "ClearanceRequest";
        public const string Finalisation = "Finalisation";
        public const string InboundError = "InboundError";

        public static string FromSoapMessageType(string? soapMessageType)
        {
            return soapMessageType switch
            {
                SoapMessageTypes.ALVSClearanceRequest => ClearanceRequest,
                SoapMessageTypes.FinalisationNotificationRequest => Finalisation,
                SoapMessageTypes.ALVSErrorNotificationRequest => InboundError,
                _ => "UnknownMessageType",
            };
        }
    }

    public static class SoapMessageTypes
    {
        public const string ALVSClearanceRequest = "ALVSClearanceRequest";
        public const string FinalisationNotificationRequest = "FinalisationNotificationRequest";
        public const string ALVSErrorNotificationRequest = "ALVSErrorNotificationRequest";
        public const string ALVSIPAFFSClearanceRequest = "ALVSClearanceRequestPost/ALVSClearanceRequest";
        public const string ALVSIPAFFSFinalisationNotificationRequest =
            "FinalisationNotificationRequestPost/FinalisationNotificationRequest";
        public const string ALVSIPAFFSDecisionNotification = "DecisionNotificationRequestPost/DecisionNotification";
    }

    public enum MessageSource
    {
        Alvs,
        Btms,
        None,
    }

    public static class Destinations
    {
        public const string BtmsCds = "BtmsCds";
    }
}
