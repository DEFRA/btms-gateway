namespace Testing;

public static class Endpoints
{
    public static class Admin
    {
        private const string Root = "/admin";

        public static string PostRedrive() => $"{Root}/redrive";
    }
}
