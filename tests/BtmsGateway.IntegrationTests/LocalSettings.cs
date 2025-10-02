namespace BtmsGateway.IntegrationTests;

public static class LocalSettings
{
    // See start-localstack.sh for queues that have this setting
    // See setting in compose.yml for Slim Message Bus options
    public static TimeSpan VisibilityTimeout => TimeSpan.FromSeconds(5);

    public static TimeSpan WaitAfterVisibilityTimeout => VisibilityTimeout.Add(TimeSpan.FromSeconds(5));
}
