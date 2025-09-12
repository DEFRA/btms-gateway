using System.Diagnostics;

namespace BtmsGateway.IntegrationTests.Helpers;

public static class AsyncWaiter
{
    private static readonly TimeSpan s_defaultDelay = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan s_defaultTimeout = TimeSpan.FromSeconds(30);

    public static async Task<bool> WaitForAsync(Func<Task<bool>> condition, TimeSpan? timeout = null)
    {
        var timer = Stopwatch.StartNew();

        while (true)
        {
            if (await condition())
                return true;

            if (timer.Elapsed > (timeout ?? s_defaultTimeout))
                return false;

            await Task.Delay(s_defaultDelay);
        }
    }
}
