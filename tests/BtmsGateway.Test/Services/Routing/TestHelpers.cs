using System.Text;
using BtmsGateway.Middleware;
using BtmsGateway.Services.Routing;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using Serilog;

namespace BtmsGateway.Test.Services.Routing;

public static class TestHelpers
{
    public static async Task<(MessageData MessageData, RoutingResult Routing)> CreateMessageData(ILogger logger)
    {
        const string Path = "http://localhost/some/path";
        var httpContext = new DefaultHttpContext();

        const string Soap =
            "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<soap:Envelope xmlns:soap=\"http://www.w3.org/2003/05/soap-envelope\" "
            + "xmlns:oas=\"http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd\">\n"
            + "<soap:Header>\n    </soap:Header>\n    <soap:Body>\n        <ALVSClearanceRequest "
            + "xmlns=\"http://submitimportdocumenthmrcfacade.types.esb.ws.cara.defra.com\">\n "
            + "<EntryReference>ALVSCDSTEST00000000688</EntryReference><DispatchCountryCode>NI</DispatchCountryCode><CorrelationId>123456789</CorrelationId>\n"
            + "</ALVSClearanceRequest>\n    </soap:Body>\n</soap:Envelope>";

        var contentStream = new MemoryStream(Encoding.UTF8.GetBytes(Soap));
        var contentType = "application/soap+xml";

        httpContext.Request.Method = "POST";
        httpContext.Request.Path = new PathString(string.Empty);
        httpContext.Request.ContentType = contentType;
        httpContext.Request.Body = contentStream;

        var logRawMessageConfigSection = Substitute.For<IConfigurationSection>();
        logRawMessageConfigSection.Value.Returns("false");

        var msgData = await MessageData.Create(httpContext.Request, logger, false);
        var routing = new RoutingResult
        {
            MessageSubXPath = "ALVSClearanceRequest",
            FullForkLink = Path,
            FullRouteLink = Path,
            ConvertRoutedContentToFromJson = true,
        };

        return (msgData, routing);
    }
}
