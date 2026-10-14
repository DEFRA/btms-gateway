using BtmsGateway.Contract.CustomsDeclarations;
using BtmsGateway.Contract.Test.Builders;

namespace BtmsGateway.Contract.Test;

public class InboundErrorTests
{
    [Fact]
    public void InboundError_should_roundtrip_json_and_xml()
    {
        var dto = new InboundError
        {
            Header = new InboundErrorHeader
            {
                EntryReference = "MRN123",
                EntryVersionNumber = 2,
                SourceCorrelationId = "SRC-1",
            },
            ServiceHeader = ServiceHeaderBuilder.Build(),
            Errors = [new InboundErrorItem { ErrorCode = "ALVS001", ErrorMessage = "Error message" }],
        };

        var result = SerialisationUtilities.RoundTripJsonAndXml(dto);

        Assert.Equal(dto.Header.EntryReference, result.Header.EntryReference);
        Assert.Equal(dto.Header.EntryVersionNumber, result.Header.EntryVersionNumber);
        Assert.Equal(dto.Header.SourceCorrelationId, result.Header.SourceCorrelationId);

        Assert.Equal(dto.ServiceHeader.SourceSystem, result.ServiceHeader.SourceSystem);
        Assert.Equal(dto.ServiceHeader.DestinationSystem, result.ServiceHeader.DestinationSystem);
        Assert.Equal(dto.ServiceHeader.CorrelationId, result.ServiceHeader.CorrelationId);
        Assert.Equal(dto.ServiceHeader.ServiceCallTimestamp, result.ServiceHeader.ServiceCallTimestamp);

        Assert.Single(result.Errors);
        Assert.Equal(dto.Errors[0].ErrorCode, result.Errors[0].ErrorCode);
        Assert.Equal(dto.Errors[0].ErrorMessage, result.Errors[0].ErrorMessage);
    }
}
