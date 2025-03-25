using System.Web;
using System.Xml;

namespace BtmsGateway.Services.Converter;

public class SoapContent
{
    public string? SoapString { get; }

    private readonly XmlNode? _soapXmlNode;

    public SoapContent(string? soapString)
    {
        SoapString = HttpUtility.HtmlDecode(soapString);
        var soapXmlNode = GetElement(SoapString);
        _soapXmlNode = soapXmlNode;
    }

    public bool HasMessage(string messageSubXPath)
    {
        return GetMessage(messageSubXPath) != null;
    }

    public string? GetMessage(string? messageSubXPath)
    {
        if (messageSubXPath == null) return null;

        var localNameXPath = MakeLocalNameXPath(messageSubXPath);
        var xpath = $"/*[local-name()='Envelope']/*[local-name()='Body']/{localNameXPath}";

        return _soapXmlNode?.SelectSingleNode(xpath)?.OuterXml;
    }

    public string? GetProperty(string? propertyXPath)
    {
        if (propertyXPath == null) return null;

        var localNameXPath = MakeLocalNameXPath(propertyXPath);
        var xpath = $"//{localNameXPath}";

        return _soapXmlNode?.SelectSingleNode(xpath)?.InnerXml;
    }

    private static string MakeLocalNameXPath(string messageSubXPath)
    {
        return string.Join('/', messageSubXPath.Trim('/').Split('/').Select(element => $"*[local-name()='{element}']"));
    }

    private static XmlNode? GetElement(string? soapString)
    {
        if (string.IsNullOrWhiteSpace(soapString)) return null;
        var doc = new XmlDocument();
        doc.LoadXml(soapString);
        return doc.DocumentElement;
    }
}