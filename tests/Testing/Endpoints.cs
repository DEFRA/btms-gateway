// ReSharper disable MemberHidesStaticFromOuterClass
namespace Testing;

public static class Endpoints
{
    public static class Redrive
    {
        private const string Root = "/admin";

        public static class DeadLetterQueue
        {
            private const string SubRoot = $"{Root}/dlq";

            public static string Redrive() => $"{SubRoot}/redrive";

            public static string RemoveMessage(string? messageId = null) =>
                $"{SubRoot}/remove-message?messageId={messageId}";

            public static string Drain() => $"{SubRoot}/drain";
        }
    }

    public static class ClearanceRequests
    {
        public static string PostClearanceRequest() => "/ITSW/CDS/SubmitImportDocumentCDSFacadeService";
    }

    public static class Finalisations
    {
        public static string PostFinalisationNotification() => "/ITSW/CDS/NotifyFinalisedStateCDSFacadeService";
    }

    public static class Errors
    {
        public static string PostInboundError() => "/ITSW/CDS/ALVSCDSErrorNotificationService";
    }

    public static class Cds
    {
        public static string PostNotification() => "/ws/CDS/defra/alvsclearanceinbound/v1";
    }

    public static class Ipaffs
    {
        public static string PostClearanceRequest() => "/soapsearch/tst/sanco/traces_ws/sendALVSClearanceRequest";

        public static string PostFinalisation() =>
            "/soapsearch/tst/sanco/traces_ws/sendFinalisationNotificationRequest";

        public static string PostDecision() => "/soapsearch/tst/sanco/traces_ws/sendALVSDecisionNotification";

        public static string PostSearchCertificate() => "/soapsearch/tst/sanco/traces_ws/searchCertificate";

        public static string PostPollSearchCertificate() =>
            "/soapsearch/tst/sanco/traces_ws/pollSearchCertificateResult";
    }

    public static class Passthrough
    {
        public static string GetHealth() => "/health";
    }
}
