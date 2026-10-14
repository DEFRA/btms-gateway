using BtmsGateway.Contract.CustomsDeclarations;
using BtmsGateway.Contract.Test.Builders;

namespace BtmsGateway.Contract.Test;

public class ClearanceRequestHeaderTests
{
    [Fact]
    public void ClearanceRequestHeader_should_roundtrip_json_and_xml()
    {
        var dto = ClearanceRequestHeaderBuilder.Build();

        var result = SerialisationUtilities.RoundTripJsonAndXml(dto);

        Assert.Equal(dto.EntryReference, result.EntryReference);
        Assert.Equal(dto.EntryVersionNumber, result.EntryVersionNumber);
        Assert.Equal(dto.PreviousVersionNumber, result.PreviousVersionNumber);
        Assert.Equal(dto.DeclarationUcr, result.DeclarationUcr);
        Assert.Equal(dto.DeclarationPartNumber, result.DeclarationPartNumber);
        Assert.Equal(dto.DeclarationType, result.DeclarationType);
        Assert.Equal(dto.ArrivalDateTime, result.ArrivalDateTime);
        Assert.Equal(dto.SubmitterTurn, result.SubmitterTurn);
        Assert.Equal(dto.DeclarantId, result.DeclarantId);
        Assert.Equal(dto.DeclarantName, result.DeclarantName);
        Assert.Equal(dto.DispatchCountryCode, result.DispatchCountryCode);
        Assert.Equal(dto.GoodsLocationCode, result.GoodsLocationCode);
        Assert.Equal(dto.MasterUcr, result.MasterUcr);
    }
}
