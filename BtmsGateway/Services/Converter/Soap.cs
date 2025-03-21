using System.Xml;

namespace BtmsGateway.Services.Converter;

public static class Soap
{
    public static string? GetMessage(string? soap, string? messageSubXPath)
    {
        if (soap == null || messageSubXPath == null) return null;

        var subXPath = string.Join('/', messageSubXPath.Trim('/').Split('/').Select(element => $"*[local-name()='{element}']"));
        var xpath = $"/*[local-name()='Envelope']/*[local-name()='Body']/{subXPath}";

        var doc = new XmlDocument();
        doc.LoadXml(soap);
        var messageNode = doc.DocumentElement?.SelectSingleNode(xpath);

        return messageNode?.InnerXml;
    }

    public static bool HasMessage(string? soap, string messageSubXPath)
    {
        return GetMessage(soap, messageSubXPath) != null;
    }
}