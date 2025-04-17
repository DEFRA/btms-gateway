using System.Xml.Linq;
using BtmsGateway.Utils;
using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;

namespace BtmsGateway.Services.Converter;

public static class ClearanceDecisionToSoapConverter
{
    public static string Convert(ClearanceDecision clearanceDecision, string mrn)
    {
        var soapContent = new List<XElement>
        {
            new XElement("ServiceHeader",
                new XElement("SourceSystem", "ALVS"),
                new XElement("DestinationSystem", "CDS"),
                new XElement("CorrelationId", clearanceDecision.ExternalCorrelationId),
                new XElement("ServiceCallTimestamp", clearanceDecision.Timestamp.ToString("yyyy-MM-ddTHH:mm:ss.sss"))
            ),
            new XElement("Header",
                new XElement("EntryReference", mrn),
                new XElement("EntryVersionNumber", clearanceDecision.ExternalVersionNumber),
                new XElement("DecisionNumber", clearanceDecision.DecisionNumber)
            )
        };

        soapContent.AddRange(clearanceDecision.Items.Select(item => new XElement("Item",
            new XElement("ItemNumber", item.ItemNumber),
            item.Checks.Select(GetCheckElement))));

        var soapBody = new XElement("DecisionNotification", soapContent);

        var soapMessage = SoapUtils.AddSoapEnvelope(soapBody, SoapType.AlvsToCds);

        var soapDocument = new XDocument(JsonToXmlConverter.XmlDeclaration, soapMessage);

        return soapDocument.ToStringWithDeclaration();
    }

    private static XElement GetCheckElement(ClearanceDecisionCheck check)
    {
        var checkElement = new XElement("Check",
            new XElement("CheckCode", check.CheckCode),
            new XElement("DecisionCode", check.DecisionCode));

        if (check.DecisionsValidUntil.HasValue)
        {
            checkElement.Add(new XElement("DecisionValidUntil", check.DecisionsValidUntil.Value.ToString("yyyyMMddHHmm")));
        }

        if (check.DecisionReasons is not null)
        {
            checkElement.Add(new XElement("DecisionReason", string.Join(", ", check.DecisionReasons)));
        }

        return checkElement;
    }
}