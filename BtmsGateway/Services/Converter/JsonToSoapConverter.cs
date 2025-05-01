using System.Diagnostics.CodeAnalysis;
using System.Xml.Linq;
using BtmsGateway.Utils;

namespace BtmsGateway.Services.Converter;

[SuppressMessage("SonarLint", "S5332", Justification = "The HTTP web links are XML namespaces so cannot change")]
public static class JsonToSoapConverter
{
    public static string Convert(string json, string rootName, SoapType soapType)
    {
        var rootElement = JsonToXmlConverter.ConvertToElement(json, rootName);
        var envelopeElement = SoapUtils.AddSoapEnvelope(rootElement, soapType);
        var soapDocument = new XDocument(JsonToXmlConverter.XmlDeclaration, envelopeElement);

        return soapDocument.ToStringWithDeclaration();
    }
}
