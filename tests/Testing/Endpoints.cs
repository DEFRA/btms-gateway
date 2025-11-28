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

    public static class Passthrough
    {
        public static string GetHealth() => "/health";
    }
}
