using System.Xml;

namespace BtmsGateway.Services.Converter;

public static class Soap
{
    public static bool HasMessage(string? soap, string messageSubXPath)
    {
        return GetMessage(soap, messageSubXPath) != null;
    }

    public static string? GetMessage(string? soap, string? messageSubXPath)
    {
        if (soap == null || messageSubXPath == null) return null;

        var subXPath = string.Join('/', messageSubXPath.Trim('/').Split('/').Select(element => $"*[local-name()='{element}']"));
        var xpath = $"/*[local-name()='Envelope']/*[local-name()='Body']/{subXPath}";

        var messageNode = GetXPathNode(soap, xpath);

        return messageNode?.OuterXml;
    }

    public static string? GetProperty(string? soap, string? propertyXPath)
    {
        if (soap == null || propertyXPath == null) return null;

        var xpath = $"//*[local-name()='{propertyXPath}']";

        var messageNode = GetXPathNode(soap, xpath);

        return messageNode?.InnerXml;
    }

    private static XmlNode? GetXPathNode(string soap, string xpath)
    {
        var doc = new XmlDocument();
        doc.LoadXml(soap);
        var messageNode = doc.DocumentElement?.SelectSingleNode(xpath);
        return messageNode;
    }
}