using System.Text;
using BtmsGateway.Middleware;
using BtmsGateway.Services.Routing;
using Microsoft.AspNetCore.Http;
using Serilog;

namespace BtmsGateway.Test.Services.Routing;

public static class TestHelpers
{

    public static async Task<(MessageData Message, RoutingResult Routing)> CreateMessageData(
        ILogger logger, bool jsonContent = true)
    {
        string path = "http://localhost/some/path";
        var httpContext = new DefaultHttpContext();

        string xml =
            "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<soap:Envelope xmlns:soap=\"http://www.w3.org/2003/05/soap-envelope\" " +
            "xmlns:oas=\"http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd\">\n" +
            "<soap:Header>\n    </soap:Header>\n    <soap:Body>\n        <ALVSClearanceRequest " +
            "xmlns=\"http://submitimportdocumenthmrcfacade.types.esb.ws.cara.defra.com\">\n " +
            "</ALVSClearanceRequest>\n    </soap:Body>\n</soap:Envelope>";

        string jsonString = "{ \"test\": \"test\" }";
        var contentStream = new MemoryStream(Encoding.UTF8.GetBytes(jsonContent ? jsonString : xml));
        var contentType = jsonContent ? "application/json" : "application/xml";

        httpContext.Request.Method = "POST";
        httpContext.Request.Path = new PathString(string.Empty);
        httpContext.Request.ContentType = contentType;
        httpContext.Request.Body = contentStream;

        var msgData = await MessageData.Create(httpContext.Request, logger);
        var routing = new RoutingResult()
        {
            FullForkLink = path,
            FullRouteLink = path,
            ConvertRoutedContentToFromJson = !jsonContent,
        };

        return (msgData, routing);
    }
}