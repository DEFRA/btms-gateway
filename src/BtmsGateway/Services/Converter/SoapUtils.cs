using System.Diagnostics.CodeAnalysis;
using System.Xml.Linq;
using BtmsGateway.Domain;

namespace BtmsGateway.Services.Converter;

[SuppressMessage("SonarLint", "S5332", Justification = "The HTTP web links are XML namespaces so cannot change")]
public static class SoapUtils
{
    private static readonly XNamespace SoapNs = "http://www.w3.org/2003/05/soap-envelope";
    private static readonly XNamespace OasNs =
        "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd";
    private static readonly XAttribute SoapNsAttribute = new(XNamespace.Xmlns + "soap", SoapNs);
    private static readonly XAttribute OasNsAttribute = new(XNamespace.Xmlns + "oas", OasNs);
    private static readonly XAttribute RoleAttribute = new(SoapNs + "role", "system");
    private static readonly XAttribute MustUnderstandAttribute = new(SoapNs + "mustUnderstand", true);
    private static readonly XAttribute PasswordTypeAttribute = new(
        "Type",
        "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-username-token-profile-1.0#PasswordText"
    );

    //ALVS to CDS Namespaces
    private static readonly XNamespace AlvsCommonRootNs = "http://uk.gov.hmrc.ITSW2.ws";
    private static readonly XAttribute AlvsSoapNsAttribute = new(XNamespace.Xmlns + "NS1", SoapNs);
    private static readonly XAttribute AlvsSecurityNsAttribute = new(XNamespace.Xmlns + "NS2", OasNs);
    private static readonly XAttribute AlvsCommonRootNsAttribute = new(XNamespace.Xmlns + "NS3", AlvsCommonRootNs);

    //HMRC requests success responses
    private static string SuccessfulAlvsClearanceResponseBody =>
        "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<soapenv:Envelope xmlns:soapenv=\"http://www.w3.org/2003/05/soap-envelope\">\n\t<soapenv:Body>\n\t\t<ALVSClearanceResponse xmlns=\"http://submitimportdocumenthmrcfacade.types.esb.ws.cara.defra.com\" xmlns:ns2=\"http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd\" xmlns:ns3=\"http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd\">\n\t\t\t<StatusCode>000</StatusCode>\n\t\t</ALVSClearanceResponse>\n\t</soapenv:Body>\n</soapenv:Envelope>";
    private static string SuccessfulFinalisationNotificationResponseBody =>
        "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<soapenv:Envelope xmlns:soapenv=\"http://www.w3.org/2003/05/soap-envelope\">\n\t<soapenv:Body>\n\t\t<FinalisationNotificationResponse xmlns=\"http://notifyfinalisedstatehmrcfacade.types.esb.ws.cara.defra.com\" xmlns:ns2=\"http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd\" xmlns:ns3=\"http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd\">\n\t\t\t<StatusCode>000</StatusCode>\n\t\t</FinalisationNotificationResponse>\n\t</soapenv:Body>\n</soapenv:Envelope>";
    private static string SuccessfulAlvsErrorNotificationResponseBody =>
        "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<soapenv:Envelope xmlns:soapenv=\"http://www.w3.org/2003/05/soap-envelope\">\n\t<soapenv:Body>\n\t\t<ALVSErrorNotificationResponse xmlns=\"http://alvserrornotification.types.esb.ws.cara.defra.com\" xmlns:ns2=\"http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd\" xmlns:ns3=\"http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd\">\n\t\t\t<StatusCode>000</StatusCode>\n\t\t</ALVSErrorNotificationResponse>\n\t</soapenv:Body>\n</soapenv:Envelope>";

    //HMRC requests failure responses
    public static string FailedSoapRequestResponseBody =>
        "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<soapenv:Envelope xmlns:soapenv=\"http://www.w3.org/2003/05/soap-envelope\">\n\t<soapenv:Body>\n\t\t<soapenv:Fault>\n\t\t\t<soapenv:Code>\n\t\t\t\t<soapenv:Value>soapenv:Receiver</soapenv:Value>\n\t\t\t</soapenv:Code>\n\t\t\t<soapenv:Reason>\n\t\t\t\t<soapenv:Text xml:lang=\"en\">A soap fault was returned.</soapenv:Text>\n\t\t\t</soapenv:Reason>\n\t\t</soapenv:Fault>\n\t</soapenv:Body>\n</soapenv:Envelope>";

    //ALVS to IPAFFS success responses
    public static string AlvsIpaffsClearanceRequestSuccessfulResponseBody =>
        "<SOAP-ENV:Envelope xmlns:SOAP-ENV=\"http://schemas.xmlsoap.org/soap/envelope/\"><SOAP-ENV:Header/><SOAP-ENV:Body><ns2:ALVSClearanceRequestPostResult xmlns:ns2=\"traceswsns\"><ns2:XMLSchemaVersion>2.0</ns2:XMLSchemaVersion><ns2:OperationCode>0</ns2:OperationCode></ns2:ALVSClearanceRequestPostResult></SOAP-ENV:Body></SOAP-ENV:Envelope>";
    public static string AlvsIpaffsFinalisationSuccessfulResponseBody =>
        "<SOAP-ENV:Envelope xmlns:SOAP-ENV=\"http://schemas.xmlsoap.org/soap/envelope/\"><SOAP-ENV:Header/><SOAP-ENV:Body><ns2:FinalisationNotificationRequestPostResult xmlns:ns2=\"traceswsns\"><ns2:XMLSchemaVersion>2.0</ns2:XMLSchemaVersion><ns2:OperationCode>0</ns2:OperationCode></ns2:FinalisationNotificationRequestPostResult></SOAP-ENV:Body></SOAP-ENV:Envelope>";
    public static string AlvsIpaffsDecisionNotificationSuccessfulResponseBody =>
        "<SOAP-ENV:Envelope xmlns:SOAP-ENV=\"http://schemas.xmlsoap.org/soap/envelope/\"><SOAP-ENV:Header/><SOAP-ENV:Body><ns2:DecisionNotificationRequestPostResult xmlns:ns2=\"traceswsns\"><ns2:XMLSchemaVersion>2.0</ns2:XMLSchemaVersion><ns2:OperationCode>0</ns2:OperationCode></ns2:DecisionNotificationRequestPostResult></SOAP-ENV:Body></SOAP-ENV:Envelope>";

    public static XElement AddSoapEnvelope(
        XElement rootElement,
        SoapType soapType,
        string? username = null,
        string? password = null
    )
    {
        return soapType switch
        {
            SoapType.Cds => GetCdsSoapEnvelope(rootElement),
            SoapType.AlvsToCds => GetAlvsToCdsSoapEnvelope(rootElement, username, password),
            SoapType.AlvsToIpaffs => GetAlvsToIpaffsSoapEnvelope(rootElement),
            _ => throw new ArgumentOutOfRangeException(nameof(soapType), soapType, "Unknown message soap type"),
        };
    }

    public static string? GetMessageTypeSuccessResponse(string? messageSubXPath)
    {
        return messageSubXPath switch
        {
            MessagingConstants.SoapMessageTypes.ALVSClearanceRequest => SuccessfulAlvsClearanceResponseBody,
            MessagingConstants.SoapMessageTypes.FinalisationNotificationRequest =>
                SuccessfulFinalisationNotificationResponseBody,
            MessagingConstants.SoapMessageTypes.ALVSErrorNotificationRequest =>
                SuccessfulAlvsErrorNotificationResponseBody,
            _ => null,
        };
    }

    private static XElement GetCdsSoapEnvelope(XElement rootElement)
    {
        XNamespace rootNs = GetRootAttributeValue(rootElement.Name.LocalName);
        return new XElement(
            SoapNs + "Envelope",
            SoapNsAttribute,
            OasNsAttribute,
            new XElement(
                SoapNs + "Header",
                new XElement(
                    OasNs + "Security",
                    RoleAttribute,
                    MustUnderstandAttribute,
                    new XElement(
                        OasNs + "UsernameToken",
                        new XElement(OasNs + "Username", "systemID=ALVSHMRCCDS,ou=gsi systems,o=defra"),
                        new XElement(OasNs + "Password", "password", PasswordTypeAttribute)
                    )
                )
            ),
            new XElement(SoapNs + "Body", AddNamespace(rootElement, rootNs))
        );
    }

    private static XElement GetAlvsToCdsSoapEnvelope(
        XElement rootElement,
        string? username = null,
        string? password = null
    )
    {
        XNamespace rootNs = GetRootAttributeValue(rootElement.Name.LocalName);

        return new XElement(
            SoapNs + "Envelope",
            AlvsSoapNsAttribute,
            new XElement(
                SoapNs + "Header",
                new XElement(
                    OasNs + "Security",
                    AlvsSecurityNsAttribute,
                    RoleAttribute,
                    new XElement(
                        OasNs + "UsernameToken",
                        new XElement(OasNs + "Username", username ?? "ibmtest"),
                        new XElement(OasNs + "Password", password ?? "password")
                    )
                )
            ),
            new XElement(
                SoapNs + "Body",
                new XElement(
                    AlvsCommonRootNs + rootElement.Name.LocalName,
                    AlvsCommonRootNsAttribute,
                    AddNamespace(rootElement, rootNs).ToString().Replace("\n", "").Replace("  ", "")
                )
            )
        );
    }

    private static XElement GetAlvsToIpaffsSoapEnvelope(XElement rootElement)
    {
        XNamespace commonRootNs = "traceswsns";
        XNamespace rootNs = GetRootAttributeValue(rootElement.Name.LocalName);
        return new XElement(
            SoapNs + "Envelope",
            SoapNsAttribute,
            new XElement(SoapNs + "Header"),
            new XElement(
                SoapNs + "Body",
                new XElement(
                    commonRootNs + $"{rootElement.Name.LocalName}Post",
                    new XElement(commonRootNs + "XMLSchemaVersion", "2.0"),
                    new XElement(commonRootNs + "UserIdentification", "username"),
                    new XElement(commonRootNs + "UserPassword", "password"),
                    new XElement(commonRootNs + "SendingDate", "2002-10-10 12:00"),
                    AddNamespace(rootElement, rootNs)
                )
            )
        );
    }

    private static XElement AddNamespace(XElement element, XNamespace rootNs)
    {
        element.Name = rootNs + element.Name.LocalName;
        foreach (var child in element.Elements())
            AddNamespace(child, rootNs);

        return element;
    }

    public static string GetRootAttributeValue(string rootName)
    {
        return rootName switch
        {
            "ALVSClearanceRequest" => "http://submitimportdocumenthmrcfacade.types.esb.ws.cara.defra.com",
            "DecisionNotification" => "http://www.hmrc.gov.uk/webservices/itsw/ws/decisionnotification",
            "FinalisationNotificationRequest" => "http://notifyfinalisedstatehmrcfacade.types.esb.ws.cara.defra.com",
            "ALVSErrorNotificationRequest" => "http://alvserrornotification.types.esb.ws.cara.defra.com",
            "HMRCErrorNotification" => "http://www.hmrc.gov.uk/webservices/itsw/ws/hmrcerrornotification",
            _ => throw new ArgumentOutOfRangeException(nameof(rootName), rootName, "Unknown message root name"),
        };
    }
}
