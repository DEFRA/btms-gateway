using BtmsGateway.Contract.CustomsDeclarations;

namespace BtmsGateway.Contract.Test;

public class DocumentTests
{
    [Fact]
    public void Document_should_roundtrip_json_and_xml()
    {
        var dto = new Document
        {
            DocumentCode = "N853",
            DocumentReference = "REF",
            DocumentStatus = "AE",
            DocumentControl = "P",
            DocumentQuantity = 123.456m,
        };

        var result = SerialisationUtilities.RoundTripJsonAndXml(dto);

        Assert.Equal(dto.DocumentCode, result.DocumentCode);
        Assert.Equal(dto.DocumentReference, result.DocumentReference);
        Assert.Equal(dto.DocumentStatus, result.DocumentStatus);
        Assert.Equal(dto.DocumentControl, result.DocumentControl);
        Assert.Equal(dto.DocumentQuantity, result.DocumentQuantity);
    }
}
