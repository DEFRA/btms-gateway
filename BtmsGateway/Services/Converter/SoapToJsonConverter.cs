using System.Xml.Linq;

namespace BtmsGateway.Services.Converter;

public static class SoapToJsonConverter
{
    public static string Convert(string xml, int messageBodyDepth)
    {
        var xContainer = ExtractXmlMessage(xml, messageBodyDepth);

        return XmlToJsonConverter.Convert(xContainer);
    }

    private static XContainer ExtractXmlMessage(string xml, int messageBodyDepth)
    {
        try
        {
            var xElement = XDocument.Parse(xml).Elements().FirstOrDefault(e => e.Name.LocalName == "Envelope")?.Elements().FirstOrDefault(e => e.Name.LocalName == "Body");
            for (var i = 0; i < messageBodyDepth; i++) 
                xElement = xElement?.Elements().LastOrDefault();
            
            return xElement ?? throw new InvalidDataException("The SOAP XML does not contain a valid message");
        }
        catch (Exception ex)
        {
            throw new ArgumentException("The XML message is not valid", ex);
        }
    }
}