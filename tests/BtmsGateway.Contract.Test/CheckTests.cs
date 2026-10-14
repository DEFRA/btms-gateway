using BtmsGateway.Contract.CustomsDeclarations;

namespace BtmsGateway.Contract.Test;

public class CheckTests
{
    [Fact]
    public void Check_should_roundtrip_json_and_xml()
    {
        var dto = new Check { CheckCode = "H222", DepartmentCode = "PHA" };

        var result = SerialisationUtilities.RoundTripJsonAndXml(dto);

        Assert.Equal(dto.CheckCode, result.CheckCode);
        Assert.Equal(dto.DepartmentCode, result.DepartmentCode);
    }
}
