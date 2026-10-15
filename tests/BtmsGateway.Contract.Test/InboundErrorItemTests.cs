using BtmsGateway.Contract.CustomsDeclarations;

namespace BtmsGateway.Contract.Test;

public class InboundErrorItemTests
{
    [Fact]
    public void InboundErrorItem_should_roundtrip_json_and_xml()
    {
        var dto = new InboundErrorItem { ErrorCode = "ALVS001", ErrorMessage = "Error message" };

        var result = SerialisationUtilities.RoundTripJsonAndXml(dto);

        Assert.Equal(dto.ErrorCode, result.ErrorCode);
        Assert.Equal(dto.ErrorMessage, result.ErrorMessage);
    }
}
