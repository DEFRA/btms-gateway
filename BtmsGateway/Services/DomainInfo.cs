using BtmsGateway.Services.Converter;

namespace BtmsGateway.Services;

public static class DomainInfo
{
    public static readonly KnownArray[] KnownArrays =
    [
        new() { ItemName = "Item", ArrayName = "Items" },
        new() { ItemName = "Document", ArrayName = "Documents" },
        new() { ItemName = "Check", ArrayName = "Checks" },
        new() { ItemName = "Error", ArrayName = "Errors" },
    ];

    public static readonly string[] KnownNumbers =
    [
        "EntryVersionNumber",
        "PreviousVersionNumber",
        "DecisionNumber",
        "ItemNumber",
        "ItemNetMass",
        "ItemSupplementaryUnits",
        "ItemThirdQuantity",
        "DocumentQuantity",
    ];
}
