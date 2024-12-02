using System.Net;
using System.Text;
using BtmsGateway.Services;

namespace BtmsGateway.Test.Utils;

public class TestHttpHandler : DelegatingHandler
{
    public const string XmlRoutedResponse = "<xml>RoutedResponse</xml>";

    public readonly Dictionary<string, HttpRequestMessage> Requests = [];
    public readonly Dictionary<string, HttpResponseMessage> Responses = [];

    private readonly Dictionary<string, Func<HttpStatusCode>> _responseStatusCodeFuncs = [];

    public void SetResponseStatusCode(string fullUrl, Func<HttpStatusCode> statusCodeFunc) => _responseStatusCodeFuncs[fullUrl] = statusCodeFunc;

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var fullUrl = request.RequestUri?.ToString()!;
        var responseStatusCodeFuncFound = _responseStatusCodeFuncs.TryGetValue(fullUrl!, out var responseStatusCodeFunc);
        var responseStatusCode = responseStatusCodeFuncFound ? responseStatusCodeFunc!() : HttpStatusCode.OK;
        if (responseStatusCode != HttpStatusCode.OK) return Task.FromResult(new HttpResponseMessage(responseStatusCode));

        Requests[fullUrl] = request;

        Responses[fullUrl] = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(XmlRoutedResponse, Encoding.UTF8, request.Content?.Headers.ContentType!)
        };
        Responses[fullUrl].Headers.Date = DateTimeOffset.UtcNow;
        Responses[fullUrl].Headers.Add(MessageData.CorrelationIdHeaderName, request.Headers.GetValues(MessageData.CorrelationIdHeaderName));
        Responses[fullUrl].Headers.Add("x-requested-path", [ request.RequestUri?.AbsolutePath ]);

        return Task.FromResult(Responses[fullUrl]);
    }
}