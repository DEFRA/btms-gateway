using System.Web;
using System.Xml.Linq;

namespace BtmsGateway.Services.Converter;

public static class SoapToJsonConverter
{
    public static string Convert(string soap, string? messageSubXPath)
    {
        var xContainer = ExtractXmlMessage(HttpUtility.HtmlDecode(soap), messageSubXPath);

        return XmlToJsonConverter.Convert(xContainer);
    }

    private static XContainer ExtractXmlMessage(string soap, string? messageSubXPath)
    {
        try
        {
            var xml = Soap.GetMessage(soap, messageSubXPath);
            if (xml == null) throw new InvalidDataException("The SOAP XML does not contain a message");

            var xElement = XElement.Parse(xml);

            return xElement ?? throw new InvalidDataException("The SOAP XML does not contain a valid message");
        }
        catch (Exception ex)
        {
            throw new ArgumentException("The XML message is not valid", ex);
        }
    }
}