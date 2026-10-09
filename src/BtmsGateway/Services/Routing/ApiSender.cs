using System.Text;
using BtmsGateway.Utils.Http;

namespace BtmsGateway.Services.Routing;

public interface IApiSender
{
    Task<HttpResponseMessage> SendSoapMessageAsync(
        string method,
        string destination,
        string contentType,
        string? hostHeader,
        IDictionary<string, string> headers,
        string soapMessage,
        CancellationToken cancellationToken
    );
}

public class ApiSender(IHttpClientFactory clientFactory) : IApiSender
{
    public async Task<HttpResponseMessage> SendSoapMessageAsync(
        string method,
        string destination,
        string contentType,
        string? hostHeader,
        IDictionary<string, string> headers,
        string soapMessage,
        CancellationToken cancellationToken
    )
    {
        var client = clientFactory.CreateClient(Proxy.CdsProxyClientWithRetry);

        var request = new HttpRequestMessage(new HttpMethod(method), destination);

        foreach (var header in headers)
        {
            request.Headers.Add(header.Key, header.Value);
        }

        if (!string.IsNullOrWhiteSpace(hostHeader))
            request.Headers.TryAddWithoutValidation("host", hostHeader);

        request.Content = new StringContent(soapMessage, Encoding.UTF8, contentType);

        return await client.SendAsync(request, cancellationToken);
    }
}
