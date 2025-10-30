using System.Net;
using System.Text;
using BtmsGateway.Middleware;
using BtmsGateway.Utils.Http;

namespace BtmsGateway.Services.Routing;

public interface IApiSender
{
    Task<RoutingResult> Send(RoutingResult routingResult, MessageData messageData);
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
    public async Task<RoutingResult> Send(RoutingResult routingResult, MessageData messageData)
    {
        var proxyName = routingResult.NamedProxy ?? Proxy.RoutedClientWithRetry;
        var client = clientFactory.CreateClient(proxyName);

        HttpRequestMessage request;

        request = routingResult.ConvertRoutedContentToFromJson
            ? messageData.CreateConvertedJsonRequest(
                routingResult.FullRouteLink,
                routingResult.RouteHostHeader,
                routingResult.MessageSubXPath
            )
            : messageData.CreateOriginalSoapRequest(routingResult.FullRouteLink, routingResult.RouteHostHeader);

        var response = await client.SendAsync(request);
        var content =
            response.StatusCode == HttpStatusCode.NoContent ? null : await response.Content.ReadAsStringAsync();

        return routingResult with
        {
            RoutingSuccessful = response.IsSuccessStatusCode,
            ResponseContent = content,
            StatusCode = response.StatusCode,
            ResponseDate = response.Headers.Date,
        };
    }

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
