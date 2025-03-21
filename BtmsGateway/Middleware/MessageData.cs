using System.Net;
using System.Net.Mime;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Amazon.SimpleNotificationService.Model;
using BtmsGateway.Services.Checking;
using BtmsGateway.Services.Converter;
using BtmsGateway.Services.Routing;
using ILogger = Serilog.ILogger;

namespace BtmsGateway.Middleware;

public class MessageData
{
    public const string CorrelationIdHeaderName = "X-Correlation-ID";
    public const string RequestedPathHeaderName = "x-requested-path";

    public string CorrelationId { get; }
    public string? OriginalContentAsString { get; }
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

    private MessageData(HttpRequest request, string? contentAsString, ILogger logger)
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
            HttpString = $"{Method} {Url} {request.Protocol.ToUpper()} {OriginalContentType}";
            var correlationId = _headers[CorrelationIdHeaderName].FirstOrDefault();
            CorrelationId = string.IsNullOrWhiteSpace(correlationId) ? Guid.NewGuid().ToString("D") : correlationId;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error constructing message data");
            throw;
        }
    }

    public bool ShouldProcessRequest => !(Method == HttpMethods.Get
                                          && (Path.StartsWith("health", StringComparison.InvariantCultureIgnoreCase)
                                              || Path.StartsWith("favicon", StringComparison.InvariantCultureIgnoreCase)
                                              || Path.StartsWith("swagger", StringComparison.InvariantCultureIgnoreCase)
                                              || Path.StartsWith(CheckRoutesEndpoints.Path, StringComparison.InvariantCultureIgnoreCase)));

    public HttpRequestMessage CreateConvertedForwardingRequest(string? routeUrl, string? hostHeader, int messageBodyDepth)
    {
        if (OriginalContentType is MediaTypeNames.Application.Xml or MediaTypeNames.Application.Soap or MediaTypeNames.Text.Xml)
        {
            var content = string.IsNullOrWhiteSpace(OriginalContentAsString)
                ? string.Empty
                : SoapToJsonConverter.Convert(OriginalContentAsString, messageBodyDepth);
            return CreateForwardingRequest(routeUrl, hostHeader, content, MediaTypeNames.Application.Json);
        }

        if (OriginalContentType is MediaTypeNames.Application.Json)
        {
            var content = string.IsNullOrWhiteSpace(OriginalContentAsString)
                ? string.Empty
                : JsonToSoapConverter.Convert(OriginalContentAsString, "FinalisationNotificationRequest", SoapType.Cds);
            return CreateForwardingRequest(routeUrl, hostHeader, content, MediaTypeNames.Application.Xml);
        }

        return CreateForwardingRequestAsOriginal(routeUrl, hostHeader);
    }

    public HttpRequestMessage CreateForwardingRequestAsOriginal(string? routeUrl, string? hostHeader)
    {
        return CreateForwardingRequest(routeUrl, hostHeader, OriginalContentAsString, OriginalContentType);
    }

    private HttpRequestMessage CreateForwardingRequest(string? routeUrl, string? hostHeader, string? contentAsString, string contentType)
    {
        try
        {
            var request = new HttpRequestMessage(new HttpMethod(Method), routeUrl);

            foreach (var header in _headers.Where(x => !x.Key.StartsWith("Content-", StringComparison.InvariantCultureIgnoreCase)
                                                       && !string.Equals(x.Key, "Accept", StringComparison.InvariantCultureIgnoreCase)
                                                       && !string.Equals(x.Key, "Host", StringComparison.InvariantCultureIgnoreCase)
                                                       && !string.Equals(x.Key, CorrelationIdHeaderName, StringComparison.InvariantCultureIgnoreCase)))
            {
                request.Headers.Add(header.Key, header.Value.ToArray());
            }
            request.Headers.Add(CorrelationIdHeaderName, CorrelationId);
            request.Headers.Add("Accept", contentType);
            if (!string.IsNullOrWhiteSpace(hostHeader)) request.Headers.TryAddWithoutValidation("host", hostHeader);

            request.Content = contentAsString == null || Method == "GET"
                ? null
                : contentType == MediaTypeNames.Application.Json
                    ? JsonContent.Create(JsonNode.Parse(string.IsNullOrWhiteSpace(contentAsString) ? "{}" : contentAsString), options: Json.SerializerOptions)
                    : new StringContent(contentAsString, Encoding.UTF8, contentType);

            return request;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error creating forwarding request");
            throw;
        }
    }

    public PublishRequest CreatePublishRequest(string? routeArn, int messageBodyDepth, string? messageGroupId = null)
    {
        var content = string.Empty;

        if (OriginalContentType is MediaTypeNames.Application.Xml or MediaTypeNames.Application.Soap or MediaTypeNames.Text.Xml)
        {
            content = string.IsNullOrWhiteSpace(OriginalContentAsString)
                ? string.Empty
                : SoapToJsonConverter.Convert(OriginalContentAsString, messageBodyDepth);
        }

        if (OriginalContentType is MediaTypeNames.Application.Json)
        {
            content = string.IsNullOrWhiteSpace(OriginalContentAsString)
                ? string.Empty
                : OriginalContentAsString;
        }

        _logger.Information("Publish JSON content to {Content}", content);

        var request = new PublishRequest
        {
            MessageGroupId = string.IsNullOrWhiteSpace(messageGroupId) ? "default" : messageGroupId,
            MessageDeduplicationId = CorrelationId,
            MessageAttributes = new Dictionary<string, MessageAttributeValue>
            {
                { "CorrelationId", new MessageAttributeValue() { StringValue = CorrelationId, DataType = "String"} }
            },
            Message = content,
            TopicArn = routeArn
        };

        return request;
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
            if (!string.IsNullOrWhiteSpace($"{routingResult.ResponseContent}{routingResult.ErrorMessage}") && response.StatusCode != (int)HttpStatusCode.NoContent)
                await response.BodyWriter.WriteAsync(new ReadOnlyMemory<byte>(Encoding.UTF8.GetBytes($"{routingResult.ResponseContent}{routingResult.ErrorMessage}")));
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error populating response");
            throw;
        }
    }

    private static async Task<string?> RetrieveContent(HttpRequest request)
    {
        if (request.Body == Stream.Null) return null;
        request.EnableBuffering();
        var content = await new StreamReader(request.Body).ReadToEndAsync();
        request.Body.Position = 0;
        return content;
    }

    private static string RetrieveContentType(HttpRequest request)
    {
        var contentTypeParts = request.ContentType?.Split(';');
        var contentType = contentTypeParts is { Length: > 0 } ? contentTypeParts[0] : null;
        if (request.Headers.Accept.Count > 0 && request.Headers.Accept[0] != "*/*") contentType ??= request.Headers.Accept[0];
        return contentType ?? "";
    }
}

public partial class ContentMap(string? content)
{
    [GeneratedRegex("CHED[A-Z]+")] private static partial Regex RegexChed();
    [GeneratedRegex("DispatchCountryCode>(.+?)<")] private static partial Regex RegexCountry();

    public string? ChedType => content == null ? null : RegexChed().Match(content).Value;
    public string? CountryCode => content == null ? null : RegexCountry().Match(content).Groups[1].Value;
}