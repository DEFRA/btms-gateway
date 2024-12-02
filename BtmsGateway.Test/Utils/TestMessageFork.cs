using BtmsGateway.Services;

namespace BtmsGateway.Test.Utils;

public class TestMessageFork : IMessageFork
{
    private readonly SemaphoreSlim _semaphoreSlim = new(0, 1);

    public TestMessageFork() => HasForked = _semaphoreSlim.AvailableWaitHandle;

    public WaitHandle HasForked { get; private set; }
    
    public void Complete() => _semaphoreSlim.Release();
}