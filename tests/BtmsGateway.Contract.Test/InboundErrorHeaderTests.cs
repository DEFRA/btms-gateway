using BtmsGateway.Contract.CustomsDeclarations;

namespace BtmsGateway.Contract.Test;

public class InboundErrorHeaderTests
{
    [Fact]
    public void InboundErrorHeader_should_roundtrip_json_and_xml()
    {
        var dto = new InboundErrorHeader
        {
            EntryReference = "MRN123",
            EntryVersionNumber = 2,
            SourceCorrelationId = "SRC-1",
        };

        var result = SerialisationUtilities.RoundTripJsonAndXml(dto);

        Assert.Equal(dto.EntryReference, result.EntryReference);
        Assert.Equal(dto.EntryVersionNumber, result.EntryVersionNumber);
        Assert.Equal(dto.SourceCorrelationId, result.SourceCorrelationId);
    }
}
