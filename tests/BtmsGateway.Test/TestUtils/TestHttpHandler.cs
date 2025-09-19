using System.Net;
using System.Text;

#nullable enable

namespace BtmsGateway.Test.TestUtils;

public class TestHttpHandler : DelegatingHandler
{
    private Func<HttpStatusCode> _responseStatusFunc = () => HttpStatusCode.OK;
    private string _responseContent = "";
    private Exception? _exceptionToThrow;

    public HttpRequestMessage? LastRequest;
    public HttpResponseMessage? LastResponse;

    public void SetNextResponse(
        string? content = null,
        Func<HttpStatusCode>? statusFunc = null,
        Exception? exceptionToThrow = null
    )
    {
        _responseStatusFunc = statusFunc ?? (() => HttpStatusCode.OK);
        _responseContent = content ?? string.Empty;
        _exceptionToThrow = exceptionToThrow;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken
    )
    {
        if (_exceptionToThrow != null)
            throw _exceptionToThrow;

        LastRequest = request;
        LastResponse = new HttpResponseMessage
        {
            StatusCode = _responseStatusFunc(),
            Content = new StringContent(_responseContent, Encoding.UTF8, request.Content?.Headers.ContentType!),
        };

        return Task.FromResult(LastResponse);
    }
}
