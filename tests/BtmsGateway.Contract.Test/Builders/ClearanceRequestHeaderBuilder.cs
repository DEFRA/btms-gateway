using BtmsGateway.Contract.CustomsDeclarations;

namespace BtmsGateway.Contract.Test.Builders;

internal static class ClearanceRequestHeaderBuilder
{
    internal static ClearanceRequestHeader Build()
    {
        return new ClearanceRequestHeader
        {
            EntryReference = "MRN123",
            EntryVersionNumber = 2,
            PreviousVersionNumber = 1,
            DeclarationUcr = "UCR-1",
            DeclarationPartNumber = "P1",
            DeclarationType = "S",
            ArrivalDateTime = new DateTime(2022, 1, 3, 12, 24, 0),
            SubmitterTurn = "123456789012",
            DeclarantId = "GB123456789013",
            DeclarantName = "Declarant",
            DispatchCountryCode = "CN",
            GoodsLocationCode = "GBSTN",
            MasterUcr = "MASTER-UCR",
        };
    }
}
