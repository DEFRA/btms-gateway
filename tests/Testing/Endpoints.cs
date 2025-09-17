namespace Testing;

public static class Endpoints
{
    public static class AdminIntegration
    {
        private const string Root = "/admin";

        public static string PostRedrive() => $"{Root}/redrive";
    }
}
