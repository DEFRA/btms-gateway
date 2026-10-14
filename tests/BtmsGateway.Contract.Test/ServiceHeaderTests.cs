using BtmsGateway.Contract.CustomsDeclarations;
using BtmsGateway.Contract.Test.Builders;

namespace BtmsGateway.Contract.Test;

public class ServiceHeaderTests
{
    [Fact]
    public void ServiceHeader_should_roundtrip_json_and_xml()
    {
        var dto = ServiceHeaderBuilder.Build();

        var result = SerialisationUtilities.RoundTripJsonAndXml(dto);

        Assert.Equal(dto.SourceSystem, result.SourceSystem);
        Assert.Equal(dto.DestinationSystem, result.DestinationSystem);
        Assert.Equal(dto.CorrelationId, result.CorrelationId);
        Assert.Equal(dto.ServiceCallTimestamp, result.ServiceCallTimestamp);
    }
}
