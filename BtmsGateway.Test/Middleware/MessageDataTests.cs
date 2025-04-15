using System.Net;
using System.Text;
using Amazon.SimpleNotificationService.Model;
using BtmsGateway.Domain;
using BtmsGateway.Middleware;
using BtmsGateway.Services.Routing;
using BtmsGateway.Test.TestUtils;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Serilog.Core;

namespace BtmsGateway.Test.Middleware;

public class MessageDataTests
{
    private const string TraceHeaderKey = "x-cdp-request-id";
    private const string RequestBody = "<Envelope><Body><Root><Data>abc</Data><CorrelationId>correlation-id</CorrelationId></Root></Body></Envelope>";

    private readonly DefaultHttpContext _httpContext = new()
    {
        Request =
        {
            Protocol = "HTTP/1.1",
            Scheme = "http",
            Method = "GET",
            Host = new HostString("localhost", 123),
            Body = new MemoryStream(Encoding.UTF8.GetBytes(RequestBody)),
            Headers =
            {
                new KeyValuePair<string, StringValues>("Content-Length", "999"),
                new KeyValuePair<string, StringValues>("X-Custom", "custom")
            }
        }
    };

    [Theory]
    [InlineData("/")]
    [InlineData("/cds")]
    [InlineData("/alvs_cds")]
    [InlineData("/alvs_ipaffs")]
    [InlineData("/test")]
    [InlineData("/simulator/cds")]
    [InlineData("/simulator/alvs_cds")]
    [InlineData("/simulator/alvs_ipaffs")]
    [InlineData("/anything")]
    public async Task When_receiving_a_get_request_that_should_be_processed_Then_it_should_indicate_so(string path)
    {
        _httpContext.Request.Path = new PathString(path);

        var messageData = await MessageData.Create(_httpContext.Request, Logger.None);

        messageData.ShouldProcessRequest.Should().BeTrue();
    }

    [Fact]
    public async Task When_creating_message_data_and_exception_occurs_Then_should_throw_the_exception()
    {
        _httpContext.Request.Path = new PathString("/");
        _httpContext.Request.Protocol = null!;

        await Assert.ThrowsAsync<NullReferenceException>(() => MessageData.Create(_httpContext.Request, Logger.None));
    }

    [Theory]
    [InlineData("/health")]
    [InlineData("/swagger")]
    public async Task When_receiving_a_post_request_that_should_be_processed_Then_it_should_indicate_so(string path)
    {
        _httpContext.Request.Method = "POST";
        _httpContext.Request.Path = new PathString(path);

        var messageData = await MessageData.Create(_httpContext.Request, Logger.None);

        messageData.ShouldProcessRequest.Should().BeTrue();
    }

    [Theory]
    [InlineData("/health")]
    [InlineData("/swagger")]
    [InlineData("/checkroutes/")]
    [InlineData("/checkroutes/json")]
    [InlineData("/checkroutes/ipaffs")]
    [InlineData("/checkroutes/ipaffs/json")]
    public async Task When_receiving_a_get_request_that_should_not_be_processed_Then_it_should_indicate_so(string path)
    {
        _httpContext.Request.Path = new PathString(path);

        var messageData = await MessageData.Create(_httpContext.Request, Logger.None);

        messageData.ShouldProcessRequest.Should().BeFalse();
    }

    [Fact]
    public async Task When_receiving_a_routable_get_request_with_accept_header_Then_it_should_break_up_the_request_parts()
    {
        _httpContext.Request.Headers.Add(new KeyValuePair<string, StringValues>("Accept", "application/json"));

        var messageData = await MessageData.Create(_httpContext.Request, Logger.None);

        messageData.OriginalContentType.Should().Be("application/json");
    }

    [Fact]
    public async Task When_creating_a_routable_get_request_without_host_header_Then_it_should_create_forwarding_request()
    {
        var messageData = await MessageData.Create(_httpContext.Request, Logger.None);

        var request = messageData.CreateOriginalSoapRequest("https://localhost:456/cds/path", null);

        request.RequestUri!.ToString().Should().Be("https://localhost:456/cds/path");
        request.Method.Should().Be(HttpMethod.Get);
        request.Version.Should().Be(Version.Parse("1.1"));
        request.Content.Should().BeNull();
        request.Headers.Count().Should().Be(2);
        request.Headers.GetValues(MessageData.CorrelationIdHeaderName).Should().BeEquivalentTo("correlation-id");
        request.Headers.GetValues("X-Custom").Should().BeEquivalentTo("custom");
    }

    [Fact]
    public async Task When_creating_a_routable_get_request_with_host_header_Then_it_should_create_forwarding_request()
    {
        var messageData = await MessageData.Create(_httpContext.Request, Logger.None);

        var request = messageData.CreateOriginalSoapRequest("https://localhost:456/cds/path", "host-header");

        request.Headers.Count().Should().Be(3);
        request.Headers.GetValues("host").Should().BeEquivalentTo("host-header");
    }

    [Fact]
    public async Task When_creating_a_routable_get_request_with_content_Then_it_should_create_forwarding_request()
    {
        _httpContext.Request.Headers.ContentType = "application/soap+xml";
        var messageData = await MessageData.Create(_httpContext.Request, Logger.None);

        var request = messageData.CreateOriginalSoapRequest("https://localhost:456/cds/path", null);

        request.Content.Should().BeNull();
        request.Headers.Count().Should().Be(3);
        request.Headers.GetValues(MessageData.CorrelationIdHeaderName).Should().BeEquivalentTo("correlation-id");
        request.Headers.GetValues("X-Custom").Should().BeEquivalentTo("custom");
        request.Headers.GetValues("Accept").Should().BeEquivalentTo("application/soap+xml");
    }

    [Fact]
    public async Task When_creating_a_routable_get_request_with_accept_header_Then_it_should_create_forwarding_request()
    {
        _httpContext.Request.Headers.Add(new KeyValuePair<string, StringValues>("Accept", "application/json"));
        var messageData = await MessageData.Create(_httpContext.Request, Logger.None);

        var request = messageData.CreateOriginalSoapRequest("https://localhost:456/cds/path", null);

        request.Headers.Count().Should().Be(3);
        request.Headers.GetValues(MessageData.CorrelationIdHeaderName).Should().BeEquivalentTo("correlation-id");
        request.Headers.GetValues("X-Custom").Should().BeEquivalentTo("custom");
        request.Headers.GetValues("Accept").Should().BeEquivalentTo("application/json");
    }

    [Fact]
    public async Task When_receiving_an_original_routable_post_request_Then_it_should_break_up_the_request_parts()
    {
        _httpContext.Request.Method = "POST";
        _httpContext.Request.Path = new PathString("/cds/path");
        _httpContext.Request.Headers.ContentType = "application/soap+xml";

        var messageData = await MessageData.Create(_httpContext.Request, Logger.None);

        messageData.HttpString.Should().Be("POST http://localhost:123/cds/path HTTP/1.1 application/soap+xml");
        messageData.Method.Should().Be("POST");
        messageData.Path.Should().Be("cds/path");
        messageData.Url.Should().Be("http://localhost:123/cds/path");
        messageData.OriginalContentType.Should().Be("application/soap+xml");
        messageData.OriginalSoapContent.SoapString?.Should().Be(RequestBody);
        messageData.ContentMap.EntryReference.Should().BeNull();
        messageData.ContentMap.CountryCode.Should().BeNull();
    }

    [Fact]
    public async Task When_receiving_an_original_routable_post_request_with_mappable_content_Then_it_should_break_up_the_request_parts()
    {
        _httpContext.Request.Method = "POST";
        _httpContext.Request.Path = new PathString("/cds/path");
        _httpContext.Request.Headers.ContentType = "application/soap+xml";
        _httpContext.Request.Body = new MemoryStream("<s:Envelope xmlns:s=\"http://soap\"><s:Body><ALVSClearanceRequest><Header><EntryReference>ALVSCDSTEST00000000688</EntryReference><DispatchCountryCode>NI</DispatchCountryCode><CorrelationId>123456789</CorrelationId></Header></ALVSClearanceRequest></s:Body></s:Envelope>"u8.ToArray());

        var messageData = await MessageData.Create(_httpContext.Request, Logger.None);

        messageData.ContentMap.EntryReference.Should().Be("ALVSCDSTEST00000000688");
        messageData.ContentMap.CountryCode.Should().Be("NI");
        messageData.ContentMap.CorrelationId.Should().Be("123456789");
    }

    [Fact]
    public async Task When_creating_a_routable_original_post_request_with_host_header_Then_it_should_create_forwarding_request()
    {
        _httpContext.Request.Method = "POST";
        _httpContext.Request.Path = new PathString("/cds/path");
        _httpContext.Request.Headers.ContentType = "application/soap+xml";
        var messageData = await MessageData.Create(_httpContext.Request, Logger.None);

        var request = messageData.CreateOriginalSoapRequest("https://localhost:456/cds/path", "host-header");

        request.RequestUri!.ToString().Should().Be("https://localhost:456/cds/path");
        request.Method.Should().Be(HttpMethod.Post);
        request.Version.Should().Be(Version.Parse("1.1"));
        (await request.Content!.ReadAsStringAsync()).Should().Be(RequestBody);
        request.Content!.Headers.ContentType!.ToString().Should().Be("application/soap+xml; charset=utf-8");
        request.Headers.Count().Should().Be(4);
        request.Headers.GetValues(MessageData.CorrelationIdHeaderName).Should().BeEquivalentTo("correlation-id");
        request.Headers.GetValues("X-Custom").Should().BeEquivalentTo("custom");
        request.Headers.GetValues("Accept").Should().BeEquivalentTo("application/soap+xml");
        request.Headers.GetValues("host").Should().BeEquivalentTo("host-header");
    }

    [Fact]
    public async Task When_creating_a_routable_converted_post_request_Then_it_should_create_forwarding_request()
    {
        _httpContext.Request.Method = "POST";
        _httpContext.Request.Path = new PathString("/cds/path");
        _httpContext.Request.Headers.ContentType = "application/soap+xml";
        var messageData = await MessageData.Create(_httpContext.Request, Logger.None);

        var request = messageData.CreateConvertedJsonRequest("https://localhost:456/cds/path", null, "Root");

        (await request.Content!.ReadAsStringAsync()).LinuxLineEndings().Should().Be("{\n  \"data\": \"abc\",\n  \"correlationId\": \"correlation-id\"\n}");
        request.Content!.Headers.ContentType!.ToString().Should().Be("application/json; charset=utf-8");
        request.Headers.Count().Should().Be(3);
        request.Headers.GetValues("Accept").Should().BeEquivalentTo("application/json");
    }

    [Fact]
    public async Task When_creating_a_routable_converted_post_request_and_exception_occurs_Then_it_should_throw_the_exception()
    {
        _httpContext.Request.Method = "";
        var messageData = await MessageData.Create(_httpContext.Request, Logger.None);

        Assert.Throws<ArgumentException>(() => messageData.CreateConvertedJsonRequest("https://localhost:456/cds/path", null, "Root"));
    }

    [Fact]
    public async Task When_populating_a_response_with_content_and_existing_date_Then_it_should_populate_response()
    {
        var responseDate = DateTimeOffset.Now;
        _httpContext.Request.Method = "POST";
        _httpContext.Request.Path = new PathString("/cds/path");
        _httpContext.Request.Headers.ContentType = "application/soap+xml";
        var messageData = await MessageData.Create(_httpContext.Request, Logger.None);
        var responseBody = new MemoryStream();
        _httpContext.Response.Body = responseBody;

        await messageData.PopulateResponse(_httpContext.Response, new RoutingResult { StatusCode = HttpStatusCode.OK, UrlPath = "cds/path", ResponseDate = responseDate, ResponseContent = "response-content" });

        _httpContext.Response.StatusCode.Should().Be(200);
        _httpContext.Response.Headers.Date[0].Should().Be(responseDate.ToString("R"));
        _httpContext.Response.Headers[MessageData.CorrelationIdHeaderName][0].Should().Be("correlation-id");
        _httpContext.Response.Headers[MessageData.RequestedPathHeaderName][0].Should().Be("cds/path");
        _httpContext.Response.ContentType.Should().Be("application/soap+xml");
        responseBody.Position = 0;
        var body = await new StreamReader(responseBody).ReadToEndAsync();
        body.Should().Be("response-content");
    }

    [Fact]
    public async Task When_populating_a_response_with_no_date_Then_it_should_populate_response()
    {
        _httpContext.Request.Method = "POST";
        _httpContext.Request.Path = new PathString("/cds/path");
        var messageData = await MessageData.Create(_httpContext.Request, Logger.None);

        await messageData.PopulateResponse(_httpContext.Response, new RoutingResult { StatusCode = HttpStatusCode.OK, UrlPath = "cds/path" });

        _httpContext.Response.StatusCode.Should().Be(200);
        _httpContext.Response.Headers.Date[0].Should().NotBeNull();
    }

    [Fact]
    public async Task When_populating_a_response_with_no_content_Then_it_should_populate_response()
    {
        _httpContext.Request.Method = "POST";
        _httpContext.Request.Path = new PathString("/cds/path");
        _httpContext.Request.Headers.ContentType = "application/soap+xml";
        var messageData = await MessageData.Create(_httpContext.Request, Logger.None);
        var responseBody = new MemoryStream();
        _httpContext.Response.Body = responseBody;

        await messageData.PopulateResponse(_httpContext.Response, new RoutingResult { StatusCode = HttpStatusCode.OK, UrlPath = "cds/path" });

        _httpContext.Response.StatusCode.Should().Be(200);
        _httpContext.Response.ContentType.Should().Be("application/soap+xml");
        responseBody.Position = 0;
        var body = await new StreamReader(responseBody).ReadToEndAsync();
        body.Should().Be("");
    }

    [Fact]
    public async Task When_populating_a_response_with_no_content_or_content_type_Then_it_should_populate_response()
    {
        _httpContext.Request.Method = "POST";
        _httpContext.Request.Path = new PathString("/cds/path");
        _httpContext.Request.Headers.ContentType = [];
        var messageData = await MessageData.Create(_httpContext.Request, Logger.None);
        var responseBody = new MemoryStream();
        _httpContext.Response.Body = responseBody;

        await messageData.PopulateResponse(_httpContext.Response, new RoutingResult { StatusCode = HttpStatusCode.OK, UrlPath = "cds/path" });

        _httpContext.Response.StatusCode.Should().Be(200);
        _httpContext.Response.ContentType.Should().BeNull();
        responseBody.Position = 0;
        var body = await new StreamReader(responseBody).ReadToEndAsync();
        body.Should().Be("");
    }

    [Fact]
    public async Task When_populating_a_no_content_response_with_no_content_Then_it_should_populate_response()
    {
        _httpContext.Request.Method = "POST";
        _httpContext.Request.Path = new PathString("/cds/path");
        _httpContext.Request.Headers.ContentType = [];
        var messageData = await MessageData.Create(_httpContext.Request, Logger.None);
        var responseBody = new MemoryStream();
        _httpContext.Response.Body = responseBody;

        await messageData.PopulateResponse(_httpContext.Response, new RoutingResult { StatusCode = HttpStatusCode.NoContent, UrlPath = "cds/path" });

        _httpContext.Response.StatusCode.Should().Be(204);
        _httpContext.Response.ContentType.Should().BeNull();
        responseBody.Position = 0;
        var body = await new StreamReader(responseBody).ReadToEndAsync();
        body.Should().Be("");
    }

    [Fact]
    public async Task When_creating_publish_request_and_trace_header_value_is_present_Then_request_should_contain_trace_header_as_message_attribute()
    {
        const string traceHeaderValue = "some-request-guid";
        _httpContext.Request.Path = new PathString("/");
        _httpContext.Request.Headers.Append(TraceHeaderKey, traceHeaderValue);
        var messageData = await MessageData.Create(_httpContext.Request, Logger.None);

        var publishRequest = messageData.CreatePublishRequest("route-arn", "Root", TraceHeaderKey);

        publishRequest.MessageAttributes.Should().ContainKey(TraceHeaderKey)
            .WhoseValue.Should().Match<MessageAttributeValue>(messageAtributeValue => messageAtributeValue.StringValue == traceHeaderValue);
    }

    [Fact]
    public async Task When_creating_publish_request_for_alvs_clearance_request_Then_request_should_contain_message_type_message_attribute()
    {
        var alvsClearanceRequest = File.ReadAllText(Path.Combine(Path.Combine("Middleware", "Fixtures"), "CdsToAlvsClearanceRequest.xml"));
        _httpContext.Request.Path = new PathString("/");
        _httpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(alvsClearanceRequest));
        var messageData = await MessageData.Create(_httpContext.Request, Logger.None);

        var publishRequest = messageData.CreatePublishRequest("route-arn", MessagingConstants.SoapMessageTypes.ALVSClearanceRequest, TraceHeaderKey);

        publishRequest.MessageAttributes.Should().ContainKey(MessagingConstants.MessageAttributeKeys.InboundHmrcMessageType)
            .WhoseValue.Should().Match<MessageAttributeValue>(messageAtributeValue => messageAtributeValue.StringValue == MessagingConstants.MessageTypes.ClearanceRequest);
    }

    [Fact]
    public async Task When_creating_publish_request_for_finalisation_notification_request_Then_request_should_contain_message_type_message_attribute()
    {
        var finalisationNotificationRequest = File.ReadAllText(Path.Combine(Path.Combine("Middleware", "Fixtures"), "CdsToAlvsFinalisationNotification.xml"));
        _httpContext.Request.Path = new PathString("/");
        _httpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(finalisationNotificationRequest));
        var messageData = await MessageData.Create(_httpContext.Request, Logger.None);

        var publishRequest = messageData.CreatePublishRequest("route-arn", MessagingConstants.SoapMessageTypes.FinalisationNotificationRequest, TraceHeaderKey);

        publishRequest.MessageAttributes.Should().ContainKey(MessagingConstants.MessageAttributeKeys.InboundHmrcMessageType)
            .WhoseValue.Should().Match<MessageAttributeValue>(messageAtributeValue => messageAtributeValue.StringValue == MessagingConstants.MessageTypes.Finalisation);
    }

    [Fact]
    public async Task When_creating_publish_request_for_error_notification_request_Then_request_should_contain_message_type_message_attribute()
    {
        var errorNotificationRequest = File.ReadAllText(Path.Combine(Path.Combine("Middleware", "Fixtures"), "AlvsErrorHandling.xml"));
        _httpContext.Request.Path = new PathString("/");
        _httpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(errorNotificationRequest));
        var messageData = await MessageData.Create(_httpContext.Request, Logger.None);

        var publishRequest = messageData.CreatePublishRequest("route-arn", MessagingConstants.SoapMessageTypes.ALVSErrorNotificationRequest, TraceHeaderKey);

        publishRequest.MessageAttributes.Should().ContainKey(MessagingConstants.MessageAttributeKeys.InboundHmrcMessageType)
            .WhoseValue.Should().Match<MessageAttributeValue>(messageAtributeValue => messageAtributeValue.StringValue == MessagingConstants.MessageTypes.InboundError);
    }

    [Fact]
    public async Task When_creating_publish_request_for_other_request_Then_request_should_not_contain_message_type_message_attribute()
    {
        var errorNotificationRequest = File.ReadAllText(Path.Combine(Path.Combine("Middleware", "Fixtures"), "CdsErrorHandling.xml"));
        _httpContext.Request.Path = new PathString("/");
        _httpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(errorNotificationRequest));
        var messageData = await MessageData.Create(_httpContext.Request, Logger.None);

        var publishRequest = messageData.CreatePublishRequest("route-arn", "HMRCErrorNotification/HMRCErrorNotification", TraceHeaderKey);

        publishRequest.MessageAttributes.Should().NotContainKey(MessagingConstants.MessageAttributeKeys.InboundHmrcMessageType);
    }
}