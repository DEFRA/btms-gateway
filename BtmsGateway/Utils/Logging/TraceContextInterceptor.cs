using Microsoft.AspNetCore.HeaderPropagation;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using SlimMessageBus;
using SlimMessageBus.Host.Interceptor;

namespace BtmsGateway.Utils.Logging;

public class TraceContextInterceptor<TMessage>(
    IOptions<TraceHeader> traceHeader,
    ITraceContextAccessor traceContextAccessor,
    HeaderPropagationValues headerPropagationValues
) : IConsumerInterceptor<TMessage>, IProducerInterceptor<TMessage>
{
    public async Task<object> OnHandle(TMessage message, Func<Task<object>> next, IConsumerContext context)
    {
        // Setting the trace context will take either the trace ID from the incoming
        // message headers or it will start a new trace ID that may be propagated onwards
        // to any nested HTTP calls or further message publishing
        traceContextAccessor.Context = new TraceContext
        {
            TraceId = context.Headers.GetTraceId(traceHeader.Value.Name) ?? Guid.NewGuid().ToString("N"),
        };

        // As per the middleware implementation for header propagation, the following sets
        // the headerPropagationValues.Headers value so it can be used by any configured
        // HTTP handler
        var headers = headerPropagationValues.Headers ??= new Dictionary<string, StringValues>(
            StringComparer.OrdinalIgnoreCase
        );

        headers.Add(traceHeader.Value.Name, traceContextAccessor.Context.TraceId);

        return await next();
    }

    public Task<object> OnHandle(TMessage message, Func<Task<object>> next, IProducerContext context)
    {
        if (!string.IsNullOrEmpty(traceContextAccessor.Context?.TraceId))
        {
            context.Headers.Add(traceHeader.Value.Name, traceContextAccessor.Context.TraceId);
        }

        return next();
    }
}
