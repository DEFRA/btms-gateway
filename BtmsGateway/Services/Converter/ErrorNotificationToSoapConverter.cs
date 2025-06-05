using System.Xml.Linq;
using BtmsGateway.Utils;
using Defra.TradeImportsDataApi.Domain.Errors;

namespace BtmsGateway.Services.Converter;

public static class ErrorNotificationToSoapConverter
{
    private const string MessageType = "HMRCErrorNotification";

    public static string Convert(ProcessingError processingError, string mrn)
    {
        var soapContent = new List<XElement>
        {
            new(
                "ServiceHeader",
                new XElement("SourceSystem", "ALVS"),
                new XElement("DestinationSystem", "CDS"),
                new XElement("CorrelationId", processingError.CorrelationId),
                new XElement("ServiceCallTimestamp", processingError.Created?.ToString("yyyy-MM-ddTHH:mm:ss.fff"))
            ),
            new(
                "Header",
                new XElement("SourceCorrelationId", processingError.SourceExternalCorrelationId),
                new XElement("EntryReference", mrn),
                new XElement("EntryVersionNumber", processingError.ExternalVersion)
            ),
        };

        soapContent.AddRange(
            processingError.Errors.Select(error => new XElement(
                "Error",
                new XElement("ErrorCode", error.Code),
                new XElement("ErrorMessage", error.Message)
            ))
        );

        XNamespace errorNotificationRootNs = SoapUtils.GetRootAttributeValue(MessageType);
        XAttribute errorNotificationRootNsAttribute = new(XNamespace.Xmlns + "NS2", errorNotificationRootNs);

        var soapBody = new XElement(
            errorNotificationRootNs + MessageType,
            errorNotificationRootNsAttribute,
            soapContent
        );

        var soapMessage = SoapUtils.AddSoapEnvelope(soapBody, SoapType.AlvsToCds);

        var soapDocument = new XDocument(new XDeclaration("1.0", "UTF-8", null), soapMessage);

        var soapString = soapDocument.ToStringWithDeclaration(errorNotificationRootNs);

        return soapString;
    }
}
