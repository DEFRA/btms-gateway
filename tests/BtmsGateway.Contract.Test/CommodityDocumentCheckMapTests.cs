using BtmsGateway.Contract.CustomsDeclarations;

namespace BtmsGateway.Contract.Test;

public class CommodityDocumentCheckMapTests
{
    [Fact]
    public void CommodityDocumentCheckMap_should_roundtrip_json_and_xml()
    {
        var dto = new CommodityDocumentCheckMap { CheckCode = "H222", DocumentCode = "N853" };

        var result = SerialisationUtilities.RoundTripJsonAndXml(dto);

        Assert.Equal(dto.CheckCode, result.CheckCode);
        Assert.Equal(dto.DocumentCode, result.DocumentCode);
    }
}
