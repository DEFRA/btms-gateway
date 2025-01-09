using BtmsGateway.Services;

namespace BtmsGateway.Test.TestUtils;

public class TestMessageForwarded : IMessageForwarded
{
    private readonly SemaphoreSlim _semaphoreRoute = new(0, 1);
    private readonly SemaphoreSlim _semaphoreFork = new(0, 1);

    public TestMessageForwarded()
    {
        HasRouted = _semaphoreRoute.AvailableWaitHandle;
        HasForked = _semaphoreFork.AvailableWaitHandle;
    }

    public WaitHandle HasRouted { get; private set; }
    public WaitHandle HasForked { get; private set; }
    
    public void Complete(ForwardedTo forwardedTo)
    {
        if (forwardedTo == ForwardedTo.Route)
            _semaphoreRoute.Release();
        else
            _semaphoreFork.Release();
    }
}