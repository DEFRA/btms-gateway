using BtmsGateway.Contract.CustomsDeclarations;
using BtmsGateway.Contract.Test.Builders;

namespace BtmsGateway.Contract.Test;

public class ItemTests
{
    [Fact]
    public void Item_should_roundtrip_json_and_xml()
    {
        var dto = ItemBuilder.Build();

        var result = SerialisationUtilities.RoundTripJsonAndXml(dto);

        Assert.Equal(dto.ItemNumber, result.ItemNumber);
        Assert.Equal(dto.CustomsProcedureCode, result.CustomsProcedureCode);
        Assert.Equal(dto.TaricCommodityCode, result.TaricCommodityCode);
        Assert.Equal(dto.GoodsDescription, result.GoodsDescription);
        Assert.Equal(dto.ConsigneeId, result.ConsigneeId);
        Assert.Equal(dto.ConsigneeName, result.ConsigneeName);
        Assert.Equal(dto.ItemNetMass, result.ItemNetMass);
        Assert.Equal(dto.ItemSupplementaryUnits, result.ItemSupplementaryUnits);
        Assert.Equal(dto.ItemThirdQuantity, result.ItemThirdQuantity);
        Assert.Equal(dto.ItemOriginCountryCode, result.ItemOriginCountryCode);

        Assert.NotNull(result.Documents);
        Assert.Single(result.Documents!);
        Assert.Equal(dto.Documents![0].DocumentCode, result.Documents![0].DocumentCode);
        Assert.Equal(dto.Documents![0].DocumentReference, result.Documents![0].DocumentReference);
        Assert.Equal(dto.Documents![0].DocumentStatus, result.Documents![0].DocumentStatus);
        Assert.Equal(dto.Documents![0].DocumentControl, result.Documents![0].DocumentControl);
        Assert.Equal(dto.Documents![0].DocumentQuantity, result.Documents![0].DocumentQuantity);

        Assert.NotNull(result.Checks);
        Assert.Single(result.Checks!);
        Assert.Equal(dto.Checks![0].CheckCode, result.Checks![0].CheckCode);
        Assert.Equal(dto.Checks![0].DepartmentCode, result.Checks![0].DepartmentCode);
    }
}
