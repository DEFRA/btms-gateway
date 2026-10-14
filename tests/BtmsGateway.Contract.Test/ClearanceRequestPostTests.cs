using BtmsGateway.Contract.CustomsDeclarations;
using BtmsGateway.Contract.Test.Builders;

namespace BtmsGateway.Contract.Test;

public class ClearanceRequestPostTests
{
    [Fact]
    public void ClearanceRequestPost_should_roundtrip_json_and_xml()
    {
        var dto = new ClearanceRequestPost
        {
            XmlSchemaVersion = "2.0",
            UserIdentification = "user",
            UserPassword = "password",
            SendingDate = new DateTime(2022, 1, 3, 12, 24, 0),
            AlvsClearanceRequest = ClearanceRequestBuilder.Build(),
        };

        var result = SerialisationUtilities.RoundTripJsonAndXml(dto);

        Assert.Equal(dto.XmlSchemaVersion, result.XmlSchemaVersion);
        Assert.Equal(dto.UserIdentification, result.UserIdentification);
        Assert.Equal(dto.UserPassword, result.UserPassword);
        Assert.Equal(dto.SendingDate, result.SendingDate);
        Assert.NotNull(result.AlvsClearanceRequest);
        Assert.Equal(
            dto.AlvsClearanceRequest!.Header.EntryReference,
            result.AlvsClearanceRequest!.Header.EntryReference
        );
        Assert.Equal(
            dto.AlvsClearanceRequest.Header.ArrivalDateTime,
            result.AlvsClearanceRequest.Header.ArrivalDateTime
        );
        Assert.Equal(dto.AlvsClearanceRequest.Items.Length, result.AlvsClearanceRequest.Items.Length);
    }
}
