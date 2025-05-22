using System.Xml.Linq;
using BtmsGateway.Utils;
using Defra.TradeImportsDataApi.Domain.Errors;

namespace BtmsGateway.Services.Converter;

public static class ErrorNotificationToSoapConverter
{
    public static string Convert(ErrorNotification errorNotification, string mrn)
    {
        var soapContent = new List<XElement>
        {
            new(
                "ServiceHeader",
                new XElement("SourceSystem", "ALVS"),
                new XElement("DestinationSystem", "CDS"),
                new XElement("CorrelationId", errorNotification.ExternalCorrelationId),
                new XElement("ServiceCallTimestamp", errorNotification.Created?.ToString("yyyy-MM-ddTHH:mm:ss.fff"))
            ),
            new(
                "Header",
                new XElement("SourceCorrelationId", errorNotification.ExternalCorrelationId),
                new XElement("EntryReference", mrn),
                new XElement("EntryVersionNumber", errorNotification.ExternalVersion)
            ),
        };

        soapContent.AddRange(
            errorNotification.Errors.Select(error => new XElement(
                "Error",
                new XElement("ErrorCode", error.Code),
                new XElement("ErrorMessage", error.Message)
            ))
        );

        var soapBody = new XElement("HMRCErrorNotification", soapContent);

        var soapMessage = SoapUtils.AddSoapEnvelope(soapBody, SoapType.AlvsToCds);

        var soapDocument = new XDocument(JsonToXmlConverter.XmlDeclaration, soapMessage);

        return soapDocument.ToStringWithDeclaration();
    }
}
