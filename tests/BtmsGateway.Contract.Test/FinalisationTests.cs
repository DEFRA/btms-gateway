using System.Xml.Serialization;
using BtmsGateway.Contract.CustomsDeclarations;
using BtmsGateway.Contract.Test.Builders;

namespace BtmsGateway.Contract.Test;

public class FinalisationTests
{
    [Fact]
    public void Finalisation_should_roundtrip_json_and_xml()
    {
        var dto = new Finalisation
        {
            Header = new FinalisationHeader
            {
                EntryReference = "MRN123",
                EntryVersionNumber = 2,
                DecisionNumber = 3,
                FinalState = "Cleared",
                ManualAction = "None",
            },
            ServiceHeader = ServiceHeaderBuilder.Build(),
        };

        var result = SerialisationUtilities.RoundTripJsonAndXml(dto);

        Assert.Equal(dto.Header.EntryReference, result.Header.EntryReference);
        Assert.Equal(dto.Header.EntryVersionNumber, result.Header.EntryVersionNumber);
        Assert.Equal(dto.Header.DecisionNumber, result.Header.DecisionNumber);
        Assert.Equal(dto.Header.FinalState, result.Header.FinalState);
        Assert.Equal(dto.Header.ManualAction, result.Header.ManualAction);

        Assert.Equal(dto.ServiceHeader.SourceSystem, result.ServiceHeader.SourceSystem);
        Assert.Equal(dto.ServiceHeader.DestinationSystem, result.ServiceHeader.DestinationSystem);
        Assert.Equal(dto.ServiceHeader.CorrelationId, result.ServiceHeader.CorrelationId);
        Assert.Equal(dto.ServiceHeader.ServiceCallTimestamp, result.ServiceHeader.ServiceCallTimestamp);
    }

    [Fact]
    public void When_deserializing_real_finalisation_xml_Then_all_properties_should_be_mapped()
    {
        const string xml = """
            <FinalisationNotificationRequest xmlns="http://notifyfinalisedstatehmrcfacade.types.esb.ws.cara.defra.com">
              <ServiceHeader>
                <SourceSystem>CHIEF</SourceSystem>
                <DestinationSystem>ALVS</DestinationSystem>
                <CorrelationId>1234567890123456789</CorrelationId>
                <ServiceCallTimestamp>2008-01-01T12:12:12.123</ServiceCallTimestamp>
              </ServiceHeader>
              <Header>
                <EntryReference>123-123456A-01/01/2008</EntryReference>
                <EntryVersionNumber>4</EntryVersionNumber>
                <DecisionNumber>3</DecisionNumber>
                <FinalState>0</FinalState>
                <ManualAction>N</ManualAction>
              </Header>
            </FinalisationNotificationRequest>
            """;

        var serializer = new XmlSerializer(
            typeof(Finalisation),
            new XmlRootAttribute("FinalisationNotificationRequest")
            {
                Namespace = "http://notifyfinalisedstatehmrcfacade.types.esb.ws.cara.defra.com",
            }
        );

        using var reader = new StringReader(xml);
        var result = (Finalisation)serializer.Deserialize(reader)!;

        Assert.Equal("CHIEF", result.ServiceHeader.SourceSystem);
        Assert.Equal("ALVS", result.ServiceHeader.DestinationSystem);
        Assert.Equal("1234567890123456789", result.ServiceHeader.CorrelationId);
        Assert.Equal(new DateTime(2008, 1, 1, 12, 12, 12, 123), result.ServiceHeader.ServiceCallTimestamp);

        Assert.Equal("123-123456A-01/01/2008", result.Header.EntryReference);
        Assert.Equal(4, result.Header.EntryVersionNumber);
        Assert.Equal(3, result.Header.DecisionNumber);
        Assert.Equal("0", result.Header.FinalState);
        Assert.Equal("N", result.Header.ManualAction);
    }
}
