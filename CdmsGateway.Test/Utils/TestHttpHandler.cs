using System.Net;
using System.Text;

namespace CdmsGateway.Test.Utils;

public class TestHttpHandler : DelegatingHandler
{
    public const string CorrelationIdHeaderName = "X-Correlation-ID";
    public const string XmlRoutedResponse = "<xml>RoutedResponse</xml>";

    public TestHttpHandler ExpectRouteUrl(string routeUrl) { _routeUrl = routeUrl; return this; }
    public TestHttpHandler ExpectRouteMethod(string routeMethod) { _routeMethod = routeMethod; return this; }
    public TestHttpHandler ExpectRouteHeaderDate(DateTimeOffset routeHeaderDate) { _routeHeaderDate = routeHeaderDate; return this; }
    public TestHttpHandler ExpectRouteHeaderCorrelationId(string routeHeaderCorrelationId) { _routeHeaderCorrelationId = routeHeaderCorrelationId; return this; }
    public TestHttpHandler ExpectRouteContentType(string routeContentType) { _routeContentType = routeContentType; return this; }
    public TestHttpHandler ExpectRouteContent(string routeContent) { _routeContent = routeContent; return this; }

    public HttpResponseMessage? Response;

    public void ShouldErrorWithStatus(Func<HttpStatusCode> statusCodeFunc) => _responseStatusCodeFunc = statusCodeFunc;

    private Func<HttpStatusCode> _responseStatusCodeFunc = () => HttpStatusCode.OK;
    
    private HttpRequestMessage? _request;
    private string? _routeUrl;
    private string? _routeMethod;
    private DateTimeOffset? _routeHeaderDate;
    private string? _routeHeaderCorrelationId;
    private string? _routeContentType;
    private string? _routeContent;
    private string? _routedContent;

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var responseStatusCode = _responseStatusCodeFunc();
        if (responseStatusCode != HttpStatusCode.OK) return new HttpResponseMessage(responseStatusCode);
        
        _request = request;

        _routedContent = await request.Content?.ReadAsStringAsync(cancellationToken)!;

        Response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(XmlRoutedResponse, Encoding.UTF8, request.Content.Headers.ContentType!)
        };
        Response.Headers.Date = DateTimeOffset.UtcNow;
        Response.Headers.Add(CorrelationIdHeaderName, request.Headers.GetValues(CorrelationIdHeaderName));
        
        return Response;
    }

    public bool WasExpectedRequestSent() => _request != null
                                            && _request.RequestUri?.ToString() == _routeUrl
                                            && _request.Method.ToString() == _routeMethod
                                            && _request.Headers.Date == _routeHeaderDate
                                            && _request.Headers.GetValues(CorrelationIdHeaderName).FirstOrDefault() == _routeHeaderCorrelationId
                                            && _request.Content?.Headers.ContentType?.ToString().StartsWith(_routeContentType!) == true
                                            && _routedContent == _routeContent;
}