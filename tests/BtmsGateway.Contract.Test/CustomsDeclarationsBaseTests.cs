using BtmsGateway.Contract.CustomsDeclarations;
using BtmsGateway.Contract.Test.Builders;

namespace BtmsGateway.Contract.Test;

public class CustomsDeclarationsBaseTests
{
    [Fact]
    public void CustomsDeclarationsBase_should_roundtrip_json_and_xml()
    {
        var dto = new CustomsDeclarationsBase { ServiceHeader = ServiceHeaderBuilder.Build() };

        var result = SerialisationUtilities.RoundTripJsonAndXml(dto);

        Assert.Equal(dto.ServiceHeader.SourceSystem, result.ServiceHeader.SourceSystem);
        Assert.Equal(dto.ServiceHeader.DestinationSystem, result.ServiceHeader.DestinationSystem);
        Assert.Equal(dto.ServiceHeader.CorrelationId, result.ServiceHeader.CorrelationId);
        Assert.Equal(dto.ServiceHeader.ServiceCallTimestamp, result.ServiceHeader.ServiceCallTimestamp);
    }
}
