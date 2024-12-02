namespace BtmsGateway.Services;

public interface IMessageFork
{
    void Complete();
}

public class MessageFork : IMessageFork
{
    public void Complete()
    {
        // Currently no action. Used for testing.
    }
}