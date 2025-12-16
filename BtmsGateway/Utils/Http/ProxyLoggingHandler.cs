using System.Diagnostics.CodeAnalysis;
using System.Net;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Http.Logging;

namespace BtmsGateway.Utils.Http;

[ExcludeFromCodeCoverage]
public class ProxyLoggingHandler(ILogger<ProxyLoggingHandler> logger) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken
    )
    {
        if (
            InnerHandler is PolicyHttpMessageHandler
            {
                InnerHandler: LoggingHttpMessageHandler { InnerHandler: HttpClientHandler handler }
            }
        )
        {
            var proxy = handler.Proxy ?? WebRequest.DefaultWebProxy;
            var proxyUri = proxy?.GetProxy(request.RequestUri!);

            logger.LogInformation(
                "HTTP {Method} {Uri} via proxy {Proxy}",
                request.Method,
                request.RequestUri,
                proxyUri ?? new Uri("direct://")
            );
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
