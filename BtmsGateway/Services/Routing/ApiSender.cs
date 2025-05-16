using System.Net;
using System.Text;
using System.Text.Json;
using BtmsGateway.Domain;
using BtmsGateway.Middleware;
using BtmsGateway.Utils.Http;
using Microsoft.AspNetCore.HeaderPropagation;
using Microsoft.Extensions.Primitives;

namespace BtmsGateway.Services.Routing;

public interface IApiSender
{
    Task<RoutingResult> Send(RoutingResult routingResult, MessageData messageData, bool fork);
    Task<HttpResponseMessage> SendSoapMessageAsync(
        string method,
        string destination,
        string contentType,
        string? hostHeader,
        IDictionary<string, string> headers,
        string soapMessage,
        CancellationToken cancellationToken
    );

    Task<HttpResponseMessage> SendDecisionAsync(
        string decision,
        string destination,
        string contentType,
        CancellationToken cancellationToken,
        IHeaderDictionary? headers = null
    );

    Task<EcsMetadata?> GetEcsMetadataAsync(CancellationToken cancellationToken);
}

public class ApiSender(IHttpClientFactory clientFactory, IServiceProvider serviceProvider, IConfiguration configuration)
    : IApiSender
{
    private static readonly JsonSerializerOptions s_jsonSerializerOptions = new();

    public async Task<RoutingResult> Send(RoutingResult routingResult, MessageData messageData, bool fork)
    {
        var client = clientFactory.CreateClient(fork ? Proxy.ForkedClientWithRetry : Proxy.RoutedClientWithRetry);

        HttpRequestMessage request;

        if (fork)
        {
            request = routingResult.ConvertForkedContentToFromJson
                ? messageData.CreateConvertedJsonRequest(
                    routingResult.FullForkLink,
                    routingResult.ForkHostHeader,
                    routingResult.MessageSubXPath
                )
                : messageData.CreateOriginalSoapRequest(routingResult.FullForkLink, routingResult.ForkHostHeader);
        }
        else
        {
            request = routingResult.ConvertRoutedContentToFromJson
                ? messageData.CreateConvertedJsonRequest(
                    routingResult.FullRouteLink,
                    routingResult.RouteHostHeader,
                    routingResult.MessageSubXPath
                )
                : messageData.CreateOriginalSoapRequest(routingResult.FullRouteLink, routingResult.RouteHostHeader);
        }

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
        var client = clientFactory.CreateClient(Proxy.ProxyClientWithRetry);

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

    public async Task<HttpResponseMessage> SendDecisionAsync(
        string decision,
        string destination,
        string contentType,
        CancellationToken cancellationToken,
        IHeaderDictionary? headers = null
    )
    {
        InitializeHeaderPropagationValues(headers);

        var client = clientFactory.CreateClient(Proxy.DecisionComparerProxyClientWithRetry);

        var request = new HttpRequestMessage(HttpMethod.Put, destination);
        request.Content = new StringContent(decision, Encoding.UTF8, contentType);

        return await client.SendAsync(request, cancellationToken);
    }

    public async Task<EcsMetadata?> GetEcsMetadataAsync(CancellationToken cancellationToken)
    {
        var metadataUri = Environment.GetEnvironmentVariable("ECS_CONTAINER_METADATA_URI_V4");

        if (string.IsNullOrEmpty(metadataUri))
            return null;

        var client = clientFactory.CreateClient(Proxy.ProxyClientWithoutRetry);
        var taskMetadataUri = metadataUri + "/task";
        var request = new HttpRequestMessage(HttpMethod.Get, taskMetadataUri);

        var response = await client.SendAsync(request, cancellationToken);

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);

        return await JsonSerializer.DeserializeAsync<EcsMetadata>(stream, s_jsonSerializerOptions, cancellationToken);
    }

    private void InitializeHeaderPropagationValues(IHeaderDictionary? headers)
    {
        var traceIdHeaderKey = configuration.GetValue<string>("TraceHeader");
        using var scope = serviceProvider.CreateScope();
        var headerPropagationValues = scope.ServiceProvider.GetRequiredService<HeaderPropagationValues>();

        var propagationHeaders = headerPropagationValues.Headers ??= new Dictionary<string, StringValues>(
            StringComparer.OrdinalIgnoreCase
        );

        if (!string.IsNullOrEmpty(traceIdHeaderKey) && !propagationHeaders.ContainsKey(traceIdHeaderKey))
        {
            var traceHeaderValue = headers is not null ? headers[traceIdHeaderKey] : StringValues.Empty;

            if (!string.IsNullOrEmpty(traceHeaderValue))
            {
                headerPropagationValues.Headers.Add(traceIdHeaderKey, traceHeaderValue);
            }
        }
    }
}
