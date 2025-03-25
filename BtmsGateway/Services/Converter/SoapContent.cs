using System.Web;
using System.Xml;

namespace BtmsGateway.Services.Converter;

public class SoapContent
{
    public string SoapString { get; }

    private readonly XmlNode _soapXmlNode;

    public SoapContent(string? soapString)
    {
        if (string.IsNullOrWhiteSpace(soapString)) throw new ArgumentNullException(soapString, "Soap content is null");
        SoapString = HttpUtility.HtmlDecode(soapString);
        var soapXmlNode = GetElement(SoapString);
        _soapXmlNode = soapXmlNode ?? throw new ArgumentNullException(SoapString, "Soap is invalid");
    }

    public bool HasMessage(string messageSubXPath)
    {
        return GetMessage(messageSubXPath) != null;
    }

    public string? GetMessage(string? messageSubXPath)
    {
        if (messageSubXPath == null) return null;

        var subXPath = string.Join('/', messageSubXPath.Trim('/').Split('/').Select(element => $"*[local-name()='{element}']"));
        var xpath = $"/*[local-name()='Envelope']/*[local-name()='Body']/{subXPath}";

        return _soapXmlNode.SelectSingleNode(xpath)?.OuterXml;
    }

    public string? GetProperty(string? propertyXPath)
    {
        if (propertyXPath == null) return null;

        var xpath = $"//*[local-name()='{propertyXPath}']";

        return _soapXmlNode.SelectSingleNode(xpath)?.InnerXml;
    }

    private static XmlNode? GetElement(string soapString)
    {
        var doc = new XmlDocument();
        doc.LoadXml(soapString);
        return doc.DocumentElement;
    }
}