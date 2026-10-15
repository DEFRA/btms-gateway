using BtmsGateway.Contract.CustomsDeclarations;

namespace BtmsGateway.Contract.Test.Builders;

internal static class ClearanceRequestBuilder
{
    internal static ClearanceRequest Build()
    {
        return new ClearanceRequest
        {
            ServiceHeader = ServiceHeaderBuilder.Build(),
            Header = ClearanceRequestHeaderBuilder.Build(),
            Items = [ItemBuilder.Build()],
        };
    }
}
