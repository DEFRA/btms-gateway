using System.Xml;
using BtmsGateway.Contract.Test.Builders;

namespace BtmsGateway.Contract.Test;

public class ServiceHeaderTests
{
    [Fact]
    public void ServiceHeader_should_roundtrip_json_and_xml()
    {
        var dto = ServiceHeaderBuilder.Build();

        var result = SerialisationUtilities.RoundTripJsonAndXml(dto);

        Assert.Equal(dto.SourceSystem, result.SourceSystem);
        Assert.Equal(dto.DestinationSystem, result.DestinationSystem);
        Assert.Equal(dto.CorrelationId, result.CorrelationId);
        Assert.Equal(dto.ServiceCallTimestamp, result.ServiceCallTimestamp);
    }

    [Fact]
    public void ServiceHeader_should_serialize_to_xml()
    {
        var dto = ServiceHeaderBuilder.Build();

        var result = SerialisationUtilities.ToXml(dto);

        var resultXml = new XmlDocument();
        resultXml.LoadXml(result);

        Assert.Equal("CHIEF", resultXml.SelectSingleNode("//SourceSystem")?.InnerText);
        Assert.Equal("ALVS", resultXml.SelectSingleNode("//DestinationSystem")?.InnerText);
        Assert.Equal("corr-1", resultXml.SelectSingleNode("//CorrelationId")?.InnerText);
        Assert.Equal("2022-01-03T12:24:00.000", resultXml.SelectSingleNode("//ServiceCallTimestamp")?.InnerText);
    }
}
