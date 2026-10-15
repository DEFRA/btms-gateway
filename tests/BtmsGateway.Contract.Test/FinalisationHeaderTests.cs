using BtmsGateway.Contract.CustomsDeclarations;

namespace BtmsGateway.Contract.Test;

public class FinalisationHeaderTests
{
    [Fact]
    public void FinalisationHeader_should_roundtrip_json_and_xml()
    {
        var dto = new FinalisationHeader
        {
            EntryReference = "MRN123",
            EntryVersionNumber = 2,
            DecisionNumber = 3,
            FinalState = "Cleared",
            ManualAction = "None",
        };

        var result = SerialisationUtilities.RoundTripJsonAndXml(dto);

        Assert.Equal(dto.EntryReference, result.EntryReference);
        Assert.Equal(dto.EntryVersionNumber, result.EntryVersionNumber);
        Assert.Equal(dto.DecisionNumber, result.DecisionNumber);
        Assert.Equal(dto.FinalState, result.FinalState);
        Assert.Equal(dto.ManualAction, result.ManualAction);
    }
}
