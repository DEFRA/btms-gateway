using BtmsGateway.Contract.CustomsDeclarations;

namespace BtmsGateway.Contract.Test;

public class ClearanceRequestPostResultTests
{
    [Fact]
    public void ClearanceRequestPostResult_should_roundtrip_json_and_xml()
    {
        var dto = new ClearanceRequestPostResult
        {
            XmlSchemaVersion = "2.0",
            SendingDate = new DateTime(2022, 1, 3, 12, 24, 0),
            OperationCode = 0,
            RequestIdentifier = "req-1",
        };

        var result = SerialisationUtilities.RoundTripJsonAndXml(dto);

        Assert.Equal(dto.XmlSchemaVersion, result.XmlSchemaVersion);
        Assert.Equal(dto.SendingDate, result.SendingDate);
        Assert.Equal(dto.OperationCode, result.OperationCode);
        Assert.Equal(dto.RequestIdentifier, result.RequestIdentifier);
    }
}
