using BtmsGateway.Contract.CustomsDeclarations;

namespace BtmsGateway.Contract.Test.Builders;

internal static class ItemBuilder
{
    internal static Item Build()
    {
        return new Item
        {
            ItemNumber = 1,
            CustomsProcedureCode = "4000000",
            TaricCommodityCode = "2309105100",
            GoodsDescription = "DOG CHEW",
            ConsigneeId = "GB123456789012",
            ConsigneeName = "Consignee",
            ItemNetMass = 5011.2m,
            ItemSupplementaryUnits = 1.5m,
            ItemThirdQuantity = 2.5m,
            ItemOriginCountryCode = "CN",
            Documents =
            [
                new Document
                {
                    DocumentCode = "N853",
                    DocumentReference = "DOC-REF",
                    DocumentStatus = "AE",
                    DocumentControl = "P",
                    DocumentQuantity = 10.5m,
                },
            ],
            Checks = [new Check { CheckCode = "H222", DepartmentCode = "PHA" }],
        };
    }
}
