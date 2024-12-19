using System.Net.Mime;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using BtmsGateway.Services.Checking;
using BtmsGateway.Services.Converter;
using BtmsGateway.Services.Routing;
using ILogger = Serilog.ILogger;

namespace BtmsGateway.Services;

public class MessageData
{
    public const string CorrelationIdHeaderName = "X-Correlation-ID";
    public const string RequestedPathHeaderName = "x-requested-path";
    
    private static readonly Dictionary<string, string> KnownArrays = new() { { "Item", "Items" }, { "Document", "Documents" }, { "Check", "Checks" } };


    public string CorrelationId { get; }
    public string OriginalContentAsString { get; }
    public string HttpString { get; }
    public string Url { get; }
    public string Path { get; }
    public string Method { get; }
    public string OriginalContentType { get; }
    public ContentMap ContentMap { get; }

    private readonly ILogger _logger;
    private readonly IHeaderDictionary _headers;

    public static async Task<MessageData> Create(HttpRequest request, ILogger logger)
    {
        var content = await RetrieveContent(request);
        return new MessageData(request, content, logger);
    }

    private MessageData(HttpRequest request, string contentAsString, ILogger logger)
    {
        _logger = logger;
        try
        {
            OriginalContentAsString = contentAsString;
            ContentMap = new ContentMap(contentAsString);
            Method = request.Method;
            Path = request.Path.HasValue ? request.Path.Value.Trim('/') : string.Empty;
            OriginalContentType = RetrieveContentType(request);
            _headers = request.Headers;
            Url = $"{request.Scheme}://{request.Host}{request.Path}{request.QueryString}";
            HttpString = $"{request.Protocol} {Method} {Url} {OriginalContentType}";       
            var correlationId = _headers[CorrelationIdHeaderName].FirstOrDefault();
            CorrelationId = string.IsNullOrWhiteSpace(correlationId) ? Guid.NewGuid().ToString("D") : correlationId;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error constructing message data");
            throw;
        }
    }

    public bool ShouldProcessRequest() => !(Method == HttpMethods.Get
                                            && (Path.Equals("health", StringComparison.CurrentCultureIgnoreCase)
                                                || Path.StartsWith("swagger", StringComparison.CurrentCultureIgnoreCase)
                                                || Path.StartsWith(CheckRoutesEndpoints.Path, StringComparison.CurrentCultureIgnoreCase)));

    public HttpRequestMessage CreateForwardingRequestAsJson(string? routeUrl)
    {
        return OriginalContentType is MediaTypeNames.Application.Xml or MediaTypeNames.Application.Soap 
            ? CreateForwardingRequest(routeUrl, string.IsNullOrWhiteSpace(OriginalContentAsString) ? string.Empty : XmlToJsonConverter.Convert(OriginalContentAsString, KnownArrays), MediaTypeNames.Application.Json) 
            : CreateForwardingRequestAsOriginal(routeUrl);
    }

    public HttpRequestMessage CreateForwardingRequestAsOriginal(string? routeUrl)
    {
        return CreateForwardingRequest(routeUrl, OriginalContentAsString, OriginalContentType);
    }

    private HttpRequestMessage CreateForwardingRequest(string? routeUrl, string contentAsString, string contentType)
    {
        try
        {
            var request = new HttpRequestMessage(new HttpMethod(Method), routeUrl);
            foreach (var header in _headers.Where(x => !x.Key.StartsWith("Content-") && x.Key != "Host" && x.Key != CorrelationIdHeaderName)) 
                request.Headers.Add(header.Key, header.Value.ToArray());
            request.Headers.Add(CorrelationIdHeaderName, CorrelationId);
        
            request.Content = contentType == MediaTypeNames.Application.Json 
                ? JsonContent.Create(JsonNode.Parse(string.IsNullOrWhiteSpace(contentAsString) ? "{}" : contentAsString)) 
                : new StringContent(contentAsString, Encoding.UTF8, contentType);

            return request;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error creating forwarding request");
            throw;
        }
    }

    public async Task PopulateResponse(HttpResponse response, RoutingResult routingResult)
    {
        try
        {
            response.StatusCode = (int)routingResult.StatusCode;
            response.ContentType = OriginalContentType;
            response.Headers.Date = (routingResult.ResponseDate ?? DateTimeOffset.Now).ToString("R");
            response.Headers[CorrelationIdHeaderName] = CorrelationId;
            response.Headers[RequestedPathHeaderName] = routingResult.UrlPath;
            if (routingResult.ResponseContent != null)
                await response.BodyWriter.WriteAsync(new ReadOnlyMemory<byte>(Encoding.UTF8.GetBytes(routingResult.ResponseContent)));
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error populating response");
            throw;
        }
    }

    private static async Task<string> RetrieveContent(HttpRequest request)
    {
        request.EnableBuffering();
        var content = await new StreamReader(request.Body).ReadToEndAsync();
        request.Body.Position = 0;
        return content;
    }

    private string RetrieveContentType(HttpRequest request)
    {
        var contentTypeParts = request.ContentType?.Split(';');
        return contentTypeParts is { Length: > 0 } ? contentTypeParts[0] : MediaTypeNames.Application.Json;
    }
}

public partial class ContentMap(string content)
{
    [GeneratedRegex("CHED[A-Z]+")] private static partial Regex RegexChed();
    [GeneratedRegex("DispatchCountryCode>(.+?)<")] private static partial Regex RegexCountry();

    public string ChedType => RegexChed().Match(content).Value;
    public string CountryCode => RegexCountry().Match(content).Groups[1].Value;
}
