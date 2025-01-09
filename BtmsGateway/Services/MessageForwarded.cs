namespace BtmsGateway.Services;

public interface IMessageForwarded
{
    void Complete(ForwardedTo forwardedTo);
}

public class MessageForwarded : IMessageForwarded
{
    public void Complete(ForwardedTo _)
    {
        // Currently no action. Used for testing.
    }
}

public enum ForwardedTo { Route, Fork }