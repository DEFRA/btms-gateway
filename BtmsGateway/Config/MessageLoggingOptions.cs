namespace BtmsGateway.Config;

public class MessageLoggingOptions
{
    public const string SectionName = nameof(MessageLoggingOptions);

    public bool LogRawMessage { get; init; }
}
