using System.Xml.Linq;

namespace BtmsGateway.Services.Converter;

public static class SoapToJsonConverter
{
    public static string Convert(string xml, Dictionary<string, string> knownArrays, int messageInBodyDepth = 1)
    {
        var xContainer = ExtractXmlMessage(xml, messageInBodyDepth);

        return XmlToJsonConverter.Convert(xContainer, knownArrays);
    }

    private static XContainer ExtractXmlMessage(string xml, int messageInBodyDepth)
    {
        try
        {
            var xElement = XDocument.Parse(xml).Elements().FirstOrDefault(e => e.Name.LocalName == "Envelope")?.Elements().FirstOrDefault(e => e.Name.LocalName == "Body");
            for (var i = 0; i < messageInBodyDepth; i++) 
                xElement = xElement?.Elements().LastOrDefault();
            
            return xElement ?? throw new InvalidDataException("The SOAP XML does not contain a valid message");
        }
        catch (Exception ex)
        {
            throw new ArgumentException("The XML message is not valid", ex);
        }
    }
}