using System.Xml.Linq;

namespace BtmsGateway.Services.Converter;

public static class SoapToJsonConverter
{
    public static string Convert(SoapContent soapContent, string? messageSubXPath)
    {
        var xContainer = ExtractXmlMessage(soapContent, messageSubXPath);

        return XmlToJsonConverter.Convert(xContainer);
    }

    private static XContainer ExtractXmlMessage(SoapContent soapContent, string? messageSubXPath)
    {
        try
        {
            var xml = soapContent.GetMessage(messageSubXPath);
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