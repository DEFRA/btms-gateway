using BtmsGateway.Contract.CustomsDeclarations;

namespace BtmsGateway.Contract.Test.Builders;

internal static class ServiceHeaderBuilder
{
    internal static ServiceHeader Build()
    {
        return new ServiceHeader
        {
            SourceSystem = "CHIEF",
            DestinationSystem = "ALVS",
            CorrelationId = "corr-1",
            ServiceCallTimestamp = new DateTime(2022, 1, 3, 12, 24, 0),
        };
    }
}
