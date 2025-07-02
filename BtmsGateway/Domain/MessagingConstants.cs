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
        public const string DecisionNotification = "DecisionNotification";
        public const string HMRCErrorNotification = "HMRCErrorNotification";
        public const string ALVSIPAFFSClearanceRequest = "ALVSIPAFFSClearanceRequest";
        public const string ALVSIPAFFSFinalisationNotificationRequest = "ALVSIPAFFSFinalisationNotificationRequest";
        public const string ALVSIPAFFSSearchCertificateRequest = "ALVSIPAFFSSearchCertificateRequest";
        public const string ALVSIPAFFSPollCertificateRequest = "ALVSIPAFFSPollCertificateRequest";
        public const string ALVSIPAFFSDecisionNotification = "ALVSIPAFFSDecisionNotification";

        public static string FromSoapMessageType(string? soapMessageType)
        {
            return soapMessageType switch
            {
                SoapMessageTypes.ALVSClearanceRequest => ClearanceRequest,
                SoapMessageTypes.FinalisationNotificationRequest => Finalisation,
                SoapMessageTypes.ALVSErrorNotificationRequest => InboundError,
                SoapMessageTypes.DecisionNotification => DecisionNotification,
                SoapMessageTypes.HMRCErrorNotification => HMRCErrorNotification,
                SoapMessageTypes.ALVSIPAFFSClearanceRequest => ALVSIPAFFSClearanceRequest,
                SoapMessageTypes.ALVSIPAFFSFinalisationNotificationRequest => ALVSIPAFFSFinalisationNotificationRequest,
                SoapMessageTypes.ALVSIPAFFSSearchCertificateRequest => ALVSIPAFFSSearchCertificateRequest,
                SoapMessageTypes.ALVSIPAFFSPollCertificateRequest => ALVSIPAFFSPollCertificateRequest,
                SoapMessageTypes.ALVSIPAFFSDecisionNotification => ALVSIPAFFSDecisionNotification,
                _ => "UnknownMessageType",
            };
        }
    }

    public static class SoapMessageTypes
    {
        public const string ALVSClearanceRequest = "ALVSClearanceRequest";
        public const string FinalisationNotificationRequest = "FinalisationNotificationRequest";
        public const string ALVSErrorNotificationRequest = "ALVSErrorNotificationRequest";
        public const string DecisionNotification = "DecisionNotification/DecisionNotification";
        public const string HMRCErrorNotification = "HMRCErrorNotification/HMRCErrorNotification";
        public const string ALVSIPAFFSClearanceRequest = "ALVSClearanceRequestPost/ALVSClearanceRequest";
        public const string ALVSIPAFFSFinalisationNotificationRequest =
            "FinalisationNotificationRequestPost/FinalisationNotificationRequest";
        public const string ALVSIPAFFSSearchCertificateRequest = "CertificateRequest/Request";
        public const string ALVSIPAFFSPollCertificateRequest = "CertificatePoll/RequestIdentifier";
        public const string ALVSIPAFFSDecisionNotification = "DecisionNotificationRequestPost/DecisionNotification";
    }

    public enum MessageSource
    {
        Alvs,
        Btms,
    }

    public static class Destinations
    {
        public const string BtmsCds = "BtmsCds";
        public const string BtmsDecisionComparer = "BtmsDecisionComparer";
        public const string AlvsDecisionComparer = "AlvsDecisionComparer";
        public const string BtmsOutboundErrors = "BtmsOutboundErrors";
        public const string AlvsOutboundErrors = "AlvsOutboundErrors";
    }
}
