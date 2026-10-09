namespace BtmsGateway.Utils.Logging;

/// <summary>
/// Pattern taken from IHttpContextAccessor implementation.
/// </summary>
public class TraceContextAccessor : ITraceContextAccessor
{
    private static readonly AsyncLocal<ContextHolder> s_contextCurrent = new();

    /// <inheritdoc/>
    public ITraceContext? Context
    {
        get => s_contextCurrent.Value?.Context;
        set
        {
            var holder = s_contextCurrent.Value;
            if (holder != null)
                holder.Context = null;

            if (value != null)
                s_contextCurrent.Value = new ContextHolder { Context = value };
        }
    }

    private sealed class ContextHolder
    {
        public ITraceContext? Context;
    }
}
