using System.Net;
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
    private readonly DefaultHttpContext _httpContext = new()
    {
        Request =
        {
            Protocol = "HTTP/1.1",
            Scheme = "http",
            Method = "GET",
            Host = new HostString("localhost", 123),
            Headers =
            {
                new KeyValuePair<string, StringValues>("Content-Length", "999"),
                new KeyValuePair<string, StringValues>("X-Correlation-ID", "correlation-id"),
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
    public async Task When_receiving_a_routable_get_request_without_content_type_or_accept_headers_Then_it_should_break_up_the_request_parts()
    {
        _httpContext.Request.Path = new PathString("/cds/path");
        _httpContext.Request.Query = new QueryCollection(new Dictionary<string, StringValues> { { "a", "b" } });

        var messageData = await MessageData.Create(_httpContext.Request, Logger.None);

        messageData.HttpString.Should().Be("GET http://localhost:123/cds/path?a=b HTTP/1.1 ");
        messageData.Method.Should().Be("GET");
        messageData.Path.Should().Be("cds/path");
        messageData.Url.Should().Be("http://localhost:123/cds/path?a=b");
        messageData.CorrelationId.Should().Be("correlation-id");
        messageData.OriginalContentType.Should().Be("");
        messageData.OriginalContentAsString.Should().BeNull();
        messageData.ContentMap.ChedType.Should().BeNull();
        messageData.ContentMap.CountryCode.Should().BeNull();
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

        var request = messageData.CreateForwardingRequestAsOriginal("https://localhost:456/cds/path", null);

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

        var request = messageData.CreateForwardingRequestAsOriginal("https://localhost:456/cds/path", "host-header");

        request.Headers.Count().Should().Be(3);
        request.Headers.GetValues("host").Should().BeEquivalentTo("host-header");
    }

    [Fact]
    public async Task When_creating_a_routable_get_request_with_content_Then_it_should_create_forwarding_request()
    {
        _httpContext.Request.Headers.ContentType = "application/soap+xml";
        _httpContext.Request.Body = new MemoryStream("<root><data>abc</data></root>"u8.ToArray());
        var messageData = await MessageData.Create(_httpContext.Request, Logger.None);

        var request = messageData.CreateForwardingRequestAsOriginal("https://localhost:456/cds/path", null);

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

        var request = messageData.CreateForwardingRequestAsOriginal("https://localhost:456/cds/path", null);

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
        _httpContext.Request.Body = new MemoryStream("<root><data>abc</data></root>"u8.ToArray());

        var messageData = await MessageData.Create(_httpContext.Request, Logger.None);

        messageData.HttpString.Should().Be("POST http://localhost:123/cds/path HTTP/1.1 application/soap+xml");
        messageData.Method.Should().Be("POST");
        messageData.Path.Should().Be("cds/path");
        messageData.Url.Should().Be("http://localhost:123/cds/path");
        messageData.CorrelationId.Should().Be("correlation-id");
        messageData.OriginalContentType.Should().Be("application/soap+xml");
        messageData.OriginalContentAsString.Should().Be("<root><data>abc</data></root>");
        messageData.ContentMap.ChedType.Should().Be("");
        messageData.ContentMap.CountryCode.Should().Be("");
    }

    [Fact]
    public async Task When_receiving_an_original_routable_post_request_with_mappable_content_Then_it_should_break_up_the_request_parts()
    {
        _httpContext.Request.Method = "POST";
        _httpContext.Request.Path = new PathString("/cds/path");
        _httpContext.Request.Headers.ContentType = "application/soap+xml";
        _httpContext.Request.Body = new MemoryStream("<root><ched>CHEDPP</ched><DispatchCountryCode>NI</DispatchCountryCode></root>"u8.ToArray());

        var messageData = await MessageData.Create(_httpContext.Request, Logger.None);

        messageData.OriginalContentAsString.Should().Be("<root><ched>CHEDPP</ched><DispatchCountryCode>NI</DispatchCountryCode></root>");
        messageData.ContentMap.ChedType.Should().Be("CHEDPP");
        messageData.ContentMap.CountryCode.Should().Be("NI");
    }

    [Fact]
    public async Task When_creating_a_routable_original_post_request_with_host_header_Then_it_should_create_forwarding_request()
    {
        _httpContext.Request.Method = "POST";
        _httpContext.Request.Path = new PathString("/cds/path");
        _httpContext.Request.Headers.ContentType = "application/soap+xml";
        _httpContext.Request.Body = new MemoryStream("<root><data>abc</data></root>"u8.ToArray());
        var messageData = await MessageData.Create(_httpContext.Request, Logger.None);

        var request = messageData.CreateForwardingRequestAsOriginal("https://localhost:456/cds/path", "host-header");

        request.RequestUri!.ToString().Should().Be("https://localhost:456/cds/path");
        request.Method.Should().Be(HttpMethod.Post);
        request.Version.Should().Be(Version.Parse("1.1"));
        (await request.Content!.ReadAsStringAsync()).Should().Be("<root><data>abc</data></root>");
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
        _httpContext.Request.Body = new MemoryStream("<Envelope><Body><root><data>abc</data></root></Body></Envelope>"u8.ToArray());
        var messageData = await MessageData.Create(_httpContext.Request, Logger.None);

        var request = messageData.CreateConvertedForwardingRequest("https://localhost:456/cds/path", null, 1);

        (await request.Content!.ReadAsStringAsync()).LinuxLineEndings().Should().Be("{\n  \"data\": \"abc\"\n}");
        request.Content!.Headers.ContentType!.ToString().Should().Be("application/json; charset=utf-8");
        request.Headers.Count().Should().Be(3);
        request.Headers.GetValues("Accept").Should().BeEquivalentTo("application/json");
    }

    [Fact]
    public async Task When_populating_a_response_with_content_and_existing_date_Then_it_should_populate_response()
    {
        var responseDate = DateTimeOffset.Now;
        _httpContext.Request.Method = "POST";
        _httpContext.Request.Path = new PathString("/cds/path");
        _httpContext.Request.Headers.ContentType = "application/soap+xml";
        _httpContext.Request.Body = new MemoryStream("<Envelope><Body><root><data>abc</data></root></Body></Envelope>"u8.ToArray());
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
}