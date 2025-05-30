using System.Diagnostics.CodeAnalysis;
using System.Xml;

namespace BtmsGateway.Services.Converter;

public class SoapContent
{
    [SuppressMessage("SonarLint", "S5332", Justification = "The HTTP web links are XML namespaces so cannot change")]
    private static readonly string[] s_htmlCodedMessageNamespaces =
    [
        "http://www.hmrc.gov.uk/webservices/itsw/ws/decisionnotification",
        "http://www.hmrc.gov.uk/webservices/itsw/ws/hmrcerrornotification",
    ];
    private static readonly string[] s_htmlCodedMessages = ["DecisionNotification", "HMRCErrorNotification"];

    public string? SoapString { get; }
    public string? RawSoapString { get; }

    private readonly XmlNode? _soapXmlNode;

    public SoapContent(string? soapString)
    {
        SoapString = GetDecodedString(soapString);
        RawSoapString = soapString;
        var soapXmlNode = GetElement(SoapString);
        _soapXmlNode = soapXmlNode;
    }

    public bool HasMessage(string messageSubXPath)
    {
        return GetMessage(messageSubXPath) != null;
    }

    public string? GetMessage(string? messageSubXPath)
    {
        if (messageSubXPath == null)
            return null;

        var localNameXPath = MakeLocalNameXPath(messageSubXPath);
        var xpath = $"/*[local-name()='Envelope']/*[local-name()='Body']/{localNameXPath}";

        return _soapXmlNode?.SelectSingleNode(xpath)?.OuterXml;
    }

    public string? GetProperty(string? propertyXPath)
    {
        if (propertyXPath == null)
            return null;

        var localNameXPath = MakeLocalNameXPath(propertyXPath);
        var xpath = $"//{localNameXPath}";

        return _soapXmlNode?.SelectSingleNode(xpath)?.InnerXml;
    }

    private static string MakeLocalNameXPath(string messageSubXPath)
    {
        return string.Join('/', messageSubXPath.Trim('/').Split('/').Select(element => $"*[local-name()='{element}']"));
    }

    private static XmlElement? GetElement(string? soapString)
    {
        if (string.IsNullOrWhiteSpace(soapString))
            return null;
        var doc = new XmlDocument();
        doc.LoadXml(soapString);
        return doc.DocumentElement;
    }

    private static string? GetDecodedString(string? soapString)
    {
        // Dealing with the raw soap string here as we don't know how to decode and load it into XML without knowing the contained message type
        if (
            soapString is not null
            && s_htmlCodedMessageNamespaces.Any(soapString.Contains)
            && s_htmlCodedMessages.Any(soapString.Contains)
        )
        {
            // Spec specifically refers to just these characters being encoded for these message types
            // Captured messages show double quotes are also encoded
            return soapString.Replace("&lt;", "<").Replace("&gt;", ">").Replace("&quot;", "\"");
        }

        return soapString;
    }
}
