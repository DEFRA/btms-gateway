namespace BtmsGateway.Contract.CustomsDeclarations;

public enum FinalStateValues
{
    // From https://eaflood.atlassian.net/wiki/spaces/ALVS/pages/5176590480/FinalState+Field
    Cleared = 0,
    CancelledAfterArrival = 1,
    CancelledWhilePreLodged = 2,
    Destroyed = 3,
    Seized = 4,
    ReleasedToKingsWarehouse = 5,
    TransferredToMss = 6,
}

public static class FinalStateExtensions
{
    public static bool IsCancelled(this FinalStateValues finalStateValues)
    {
        return finalStateValues is FinalStateValues.CancelledAfterArrival or FinalStateValues.CancelledWhilePreLodged;
    }

    public static bool IsNotCancelled(this FinalStateValues finalStateValues)
    {
        return !finalStateValues.IsCancelled();
    }
}
