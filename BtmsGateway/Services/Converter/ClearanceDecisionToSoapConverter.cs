using System.Xml.Linq;
using BtmsGateway.Utils;
using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;

namespace BtmsGateway.Services.Converter;

public static class ClearanceDecisionToSoapConverter
{
    private const string MessageType = "DecisionNotification";

    public static string Convert(ClearanceDecision clearanceDecision, string mrn)
    {
        var soapContent = new List<XElement>
        {
            new XElement(
                "ServiceHeader",
                new XElement("SourceSystem", "ALVS"),
                new XElement("DestinationSystem", "CDS"),
                new XElement("CorrelationId", clearanceDecision.CorrelationId),
                new XElement("ServiceCallTimestamp", clearanceDecision.Created.ToString("yyyy-MM-ddTHH:mm:ss.fff"))
            ),
            new XElement(
                "Header",
                new XElement("EntryReference", mrn),
                new XElement("EntryVersionNumber", clearanceDecision.ExternalVersionNumber),
                new XElement("DecisionNumber", clearanceDecision.DecisionNumber)
            ),
        };

        soapContent.AddRange(
            clearanceDecision.Items.Select(item => new XElement(
                "Item",
                new XElement("ItemNumber", item.ItemNumber),
                item.Checks.Select(GetCheckElement)
            ))
        );

        XNamespace decisionNotificationRootNs = SoapUtils.GetRootAttributeValue(MessageType);
        XAttribute decisionNotificationRootNsAttribute = new(XNamespace.Xmlns + "NS2", decisionNotificationRootNs);

        var soapBody = new XElement(
            decisionNotificationRootNs + MessageType,
            decisionNotificationRootNsAttribute,
            soapContent
        );

        var soapMessage = SoapUtils.AddSoapEnvelope(soapBody, SoapType.AlvsToCds);

        var soapDocument = new XDocument(new XDeclaration("1.0", "UTF-8", null), soapMessage);

        var soapString = soapDocument.ToStringWithDeclaration(decisionNotificationRootNs);

        return soapString;
    }

    private static XElement GetCheckElement(ClearanceDecisionCheck check)
    {
        var checkElement = new XElement(
            "Check",
            new XElement("CheckCode", check.CheckCode),
            new XElement("DecisionCode", check.DecisionCode)
        );

        if (check.DecisionsValidUntil.HasValue)
        {
            checkElement.Add(
                new XElement("DecisionValidUntil", check.DecisionsValidUntil.Value.ToString("yyyyMMddHHmm"))
            );
        }

        if (check.DecisionReasons is null)
            return checkElement;

        foreach (var checkDecisionReason in check.DecisionReasons)
        {
            if (!string.IsNullOrEmpty(checkDecisionReason))
                checkElement.Add(new XElement("DecisionReason", checkDecisionReason));
        }

        return checkElement;
    }
}
