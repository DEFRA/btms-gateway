using System.Net;
using System.Net.Mime;
using System.Text;
using System.Text.Json.Nodes;
using Amazon.SimpleNotificationService.Model;
using BtmsGateway.Domain;
using BtmsGateway.Services.Checking;
using BtmsGateway.Services.Converter;
using BtmsGateway.Services.Routing;
using ILogger = Serilog.ILogger;

namespace BtmsGateway.Middleware;

public class MessageData
{
    public const string CorrelationIdHeaderName = "CorrelationId";
    public const string RequestedPathHeaderName = "x-requested-path";

    public SoapContent OriginalSoapContent { get; }
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
            OriginalSoapContent = new SoapContent(contentAsString);
            ContentMap = new ContentMap(OriginalSoapContent);
            Method = request.Method;
            Path = request.Path.HasValue ? request.Path.Value.Trim('/') : string.Empty;
            OriginalContentType = RetrieveContentType(request);
            _headers = request.Headers;
            Url = $"{request.Scheme}://{request.Host}{request.Path}{request.QueryString}";
            HttpString = $"{Method} {Url} {request.Protocol.ToUpper()} {OriginalContentType}";
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

    public HttpRequestMessage CreateConvertedJsonRequest(string? routeUrl, string? hostHeader, string? messageSubXPath)
    {
        var content = SoapToJsonConverter.Convert(OriginalSoapContent, messageSubXPath);
        return CreateForwardingRequest(routeUrl, hostHeader, content, MediaTypeNames.Application.Json);
    }

    public HttpRequestMessage CreateOriginalSoapRequest(string? routeUrl, string? hostHeader)
    {
        return CreateForwardingRequest(routeUrl, hostHeader, OriginalSoapContent.SoapString, OriginalContentType);
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
            request.Headers.Add(CorrelationIdHeaderName, ContentMap.CorrelationId);
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

    public PublishRequest CreatePublishRequest(string? routeArn, string? messageSubXPath, string? traceHeaderKey)
    {
        var content = SoapToJsonConverter.Convert(OriginalSoapContent, messageSubXPath);

        _logger.Debug("{ContentCorrelationId} Publish JSON content", ContentMap.CorrelationId);

        var request = new PublishRequest
        {
            MessageGroupId = ContentMap.EntryReference,
            MessageDeduplicationId = ContentMap.CorrelationId,
            MessageAttributes = GetMessageAttributes(messageSubXPath),
            Message = content,
            TopicArn = routeArn
        };

        if (!string.IsNullOrEmpty(traceHeaderKey))
        {
            if (!string.IsNullOrEmpty(_headers[traceHeaderKey]))
            {
                string traceValue = _headers[traceHeaderKey];
                request.MessageAttributes.Add(traceHeaderKey, new MessageAttributeValue { StringValue = traceValue, DataType = "String" });
                _logger.Debug("{ContentCorrelationId} TraceHeaderKey found and set to {TraceValue}", ContentMap.CorrelationId, traceValue);
            }
            else
            {
                _logger.Debug("{ContentCorrelationId} TraceHeaderKey not found {TraceHeaderKey}", ContentMap.CorrelationId, traceHeaderKey);
            }
        }

        return request;
    }

    public async Task PopulateResponse(HttpResponse response, RoutingResult routingResult)
    {
        try
        {
            response.StatusCode = (int)routingResult.StatusCode;
            response.ContentType = OriginalContentType;
            response.Headers.Date = (routingResult.ResponseDate ?? DateTimeOffset.Now).ToString("R");
            response.Headers[CorrelationIdHeaderName] = ContentMap.CorrelationId;
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

    private Dictionary<string, MessageAttributeValue> GetMessageAttributes(string? messageSubXPath)
    {
        var messageAttributes = new Dictionary<string, MessageAttributeValue>
        {
            {
                MessagingConstants.MessageAttributeKeys.CorrelationId,
                new MessageAttributeValue { StringValue = ContentMap.CorrelationId, DataType = "String" }
            }
        };

        var attributeValue = messageSubXPath switch
        {
            MessagingConstants.SoapMessageTypes.ALVSClearanceRequest => MessagingConstants.MessageTypes.ClearanceRequest,
            MessagingConstants.SoapMessageTypes.FinalisationNotificationRequest => MessagingConstants.MessageTypes.Finalisation,
            MessagingConstants.SoapMessageTypes.ALVSErrorNotificationRequest => MessagingConstants.MessageTypes.InboundError,
            _ => string.Empty
        };

        if (!string.IsNullOrWhiteSpace(attributeValue))
        {
            messageAttributes.Add(MessagingConstants.MessageAttributeKeys.MessageType,
                new MessageAttributeValue { DataType = "String", StringValue = attributeValue });
            _logger.Debug("{ContentCorrelationId} Message Type Attribute Value {AttributeValue} added for SOAP message type {SOAPMessageType}",
                ContentMap.CorrelationId,
                attributeValue,
                messageSubXPath);
        }
        else
        {
            _logger.Debug("{ContentCorrelationId} Message Type Attribute Value not added for SOAP message type {SOAPMessageType}",
                ContentMap.CorrelationId,
                messageSubXPath);
        }

        return messageAttributes;
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

public class ContentMap(SoapContent soapContent)
{
    public string? EntryReference => soapContent.GetProperty("EntryReference");
    public string? CountryCode => soapContent.GetProperty("DispatchCountryCode");
    public string? CorrelationId => soapContent.GetProperty("CorrelationId");
    public string? RequestIdentifier => soapContent.GetProperty("RequestIdentifier");
    public string? MessageReference => EntryReference ?? RequestIdentifier ?? "NO MESSAGE REFERENCE";
}