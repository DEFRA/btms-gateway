using BtmsGateway.Contract.CustomsDeclarations;
using BtmsGateway.Contract.Test.Builders;

namespace BtmsGateway.Contract.Test;

public class CustomsDeclarationsMessageTests
{
    [Fact]
    public void CustomsDeclarationsMessage_should_roundtrip_json_and_xml()
    {
        var dto = new CustomsDeclarationsMessage
        {
            ServiceHeader = ServiceHeaderBuilder.Build(),
            Header = new Header { EntryReference = "MRN123", EntryVersionNumber = 2 },
        };

        var result = SerialisationUtilities.RoundTripJsonAndXml(dto);

        Assert.Equal(dto.ServiceHeader.SourceSystem, result.ServiceHeader.SourceSystem);
        Assert.Equal(dto.ServiceHeader.DestinationSystem, result.ServiceHeader.DestinationSystem);
        Assert.Equal(dto.ServiceHeader.CorrelationId, result.ServiceHeader.CorrelationId);
        Assert.Equal(dto.ServiceHeader.ServiceCallTimestamp, result.ServiceHeader.ServiceCallTimestamp);
        Assert.Equal(dto.Header.EntryReference, result.Header.EntryReference);
        Assert.Equal(dto.Header.EntryVersionNumber, result.Header.EntryVersionNumber);
    }
}
