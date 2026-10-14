using BtmsGateway.Contract.CustomsDeclarations;

namespace BtmsGateway.Contract.Test;

public class HeaderTests
{
    [Fact]
    public void Header_should_roundtrip_json_and_xml()
    {
        var dto = new Header { EntryReference = "MRN123", EntryVersionNumber = 2 };

        var result = SerialisationUtilities.RoundTripJsonAndXml(dto);

        Assert.Equal(dto.EntryReference, result.EntryReference);
        Assert.Equal(dto.EntryVersionNumber, result.EntryVersionNumber);
    }
}
