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
}
