using System.Net;
using System.Text;

#nullable enable

namespace BtmsGateway.Test.TestUtils;

public class TestHttpHandler : DelegatingHandler
{
    private Func<HttpStatusCode> _responseStatusFunc = () => HttpStatusCode.OK;
    private string _responseContent = "";

    public HttpRequestMessage? LastRequest;
    public HttpResponseMessage? LastResponse;

    public void SetNextResponse(string? content = null, Func<HttpStatusCode>? statusFunc = null)
    {
        _responseStatusFunc = statusFunc ?? (() => HttpStatusCode.OK);
        _responseContent = content ?? string.Empty;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        LastRequest = request;
        LastResponse = new HttpResponseMessage
        {
            StatusCode = _responseStatusFunc(),
            Content = new StringContent(_responseContent, Encoding.UTF8, request.Content?.Headers.ContentType!)
        };
        
        return Task.FromResult(LastResponse);
    }
}