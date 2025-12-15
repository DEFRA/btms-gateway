using System.Diagnostics.CodeAnalysis;
using System.Net;

namespace BtmsGateway.Utils.Http;

[ExcludeFromCodeCoverage]
public class ProxyLoggingHandler(ILogger<ProxyLoggingHandler> logger) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken
    )
    {
        if (InnerHandler is HttpClientHandler handler)
        {
            var proxy = handler.Proxy ?? WebRequest.DefaultWebProxy;
            var proxyUri = proxy?.GetProxy(request.RequestUri);

            logger.LogInformation("Request {RequestUri} → Proxy {ProxyUri}", request.RequestUri, proxyUri);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
