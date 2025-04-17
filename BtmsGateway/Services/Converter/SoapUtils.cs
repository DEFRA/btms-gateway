using System.Diagnostics.CodeAnalysis;
using System.Xml.Linq;

namespace BtmsGateway.Services.Converter;

[SuppressMessage("SonarLint", "S5332", Justification = "The HTTP web links are XML namespaces so cannot change")]
public static class SoapUtils
{
    private static readonly XNamespace SoapNs = "http://www.w3.org/2003/05/soap-envelope";
    private static readonly XNamespace OasNs = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd";
    private static readonly XAttribute SoapNsAttribute = new(XNamespace.Xmlns + "soap", SoapNs);
    private static readonly XAttribute OasNsAttribute = new(XNamespace.Xmlns + "oas", OasNs);
    private static readonly XAttribute RoleAttribute = new(SoapNs + "role", "system");
    private static readonly XAttribute MustUnderstandAttribute = new(SoapNs + "mustUnderstand", true);
    private static readonly XAttribute PasswordTypeAttribute = new("Type", "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-username-token-profile-1.0#PasswordText");

    public static XElement AddSoapEnvelope(XElement rootElement, SoapType soapType)
    {
        return soapType switch
        {
            SoapType.Cds => GetCdsSoapEnvelope(rootElement),
            SoapType.AlvsToCds => GetAlvsToCdsSoapEnvelope(rootElement),
            SoapType.AlvsToIpaffs => GetAlvsToIpaffsSoapEnvelope(rootElement),
            _ => throw new ArgumentOutOfRangeException(nameof(soapType), soapType, "Unknown message soap type")
        };
    }

    private static XElement GetCdsSoapEnvelope(XElement rootElement)
    {
        XNamespace rootNs = GetRootAttributeValue(rootElement.Name.LocalName);
        return new XElement(SoapNs + "Envelope", SoapNsAttribute, OasNsAttribute,
            new XElement(SoapNs + "Header",
                new XElement(OasNs + "Security", RoleAttribute, MustUnderstandAttribute,
                    new XElement(OasNs + "UsernameToken",
                        new XElement(OasNs + "Username", "systemID=ALVSHMRCCDS,ou=gsi systems,o=defra"),
                        new XElement(OasNs + "Password", "password", PasswordTypeAttribute)))),
            new XElement(SoapNs + "Body",
                AddNamespace(rootElement, rootNs)));
    }

    private static XElement GetAlvsToCdsSoapEnvelope(XElement rootElement)
    {
        XNamespace commonRootNs = "http://uk.gov.hmrc.ITSW2.ws";
        XNamespace rootNs = GetRootAttributeValue(rootElement.Name.LocalName);
        return new XElement(SoapNs + "Envelope", SoapNsAttribute,
            new XElement(SoapNs + "Header",
                new XElement(OasNs + "Security", RoleAttribute, MustUnderstandAttribute, OasNsAttribute,
                    new XElement(OasNs + "UsernameToken",
                        new XElement(OasNs + "Username", "ibmtest"),
                        new XElement(OasNs + "Password", "password")))),
            new XElement(SoapNs + "Body",
                new XElement(commonRootNs + rootElement.Name.LocalName,
                    AddNamespace(rootElement, rootNs))));
    }

    private static XElement GetAlvsToIpaffsSoapEnvelope(XElement rootElement)
    {
        XNamespace commonRootNs = "traceswsns";
        XNamespace rootNs = GetRootAttributeValue(rootElement.Name.LocalName);
        return new XElement(SoapNs + "Envelope", SoapNsAttribute,
            new XElement(SoapNs + "Header"),
            new XElement(SoapNs + "Body",
                new XElement(commonRootNs + $"{rootElement.Name.LocalName}Post",
                    new XElement(commonRootNs + "XMLSchemaVersion", "2.0"),
                    new XElement(commonRootNs + "UserIdentification", "username"),
                    new XElement(commonRootNs + "UserPassword", "password"),
                    new XElement(commonRootNs + "SendingDate", "2002-10-10 12:00"),
                    AddNamespace(rootElement, rootNs))));
    }

    private static XElement AddNamespace(XElement element, XNamespace rootNs)
    {
        element.Name = rootNs + element.Name.LocalName;
        foreach (var child in element.Elements())
            AddNamespace(child, rootNs);

        return element;
    }

    private static string GetRootAttributeValue(string rootName)
    {
        return rootName switch
        {
            "ALVSClearanceRequest" => "http://submitimportdocumenthmrcfacade.types.esb.ws.cara.defra.com",
            "DecisionNotification" => "http://www.hmrc.gov.uk/webservices/itsw/ws/decisionnotification",
            "FinalisationNotificationRequest" => "http://notifyfinalisedstatehmrcfacade.types.esb.ws.cara.defra.com",
            "ALVSErrorNotificationRequest" => "http://alvserrornotification.types.esb.ws.cara.defra.com",
            "HMRCErrorNotification" => "http://hmrcerror.types.esb.ws.cara.defra.com",
            _ => throw new ArgumentOutOfRangeException(nameof(rootName), rootName, "Unknown message root name")
        };
    }
}