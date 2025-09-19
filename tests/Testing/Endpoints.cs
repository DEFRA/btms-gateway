namespace Testing;

public static class Endpoints
{
    public static class AdminIntegration
    {
        private const string Root = "/admin";

        public static string PostRedrive() => $"{Root}/redrive";
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
