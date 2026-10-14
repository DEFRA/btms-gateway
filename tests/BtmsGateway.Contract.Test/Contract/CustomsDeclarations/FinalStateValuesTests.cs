using AwesomeAssertions;
using BtmsGateway.Contract.CustomsDeclarations;

namespace BtmsGateway.Contract.Test.Contract.CustomsDeclarations;

public class FinalStateValuesTests
{
    [Theory]
    [InlineData(FinalStateValues.CancelledAfterArrival)]
    [InlineData(FinalStateValues.CancelledWhilePreLodged)]
    public void When_state_is_cancelled_Then_is_cancelled_should_be_true(FinalStateValues value)
    {
        value.IsCancelled().Should().BeTrue();
        value.IsNotCancelled().Should().BeFalse();
    }

    [Theory]
    [InlineData(FinalStateValues.Cleared)]
    [InlineData(FinalStateValues.Destroyed)]
    [InlineData(FinalStateValues.Seized)]
    [InlineData(FinalStateValues.ReleasedToKingsWarehouse)]
    [InlineData(FinalStateValues.TransferredToMss)]
    public void When_state_is_not_cancelled_Then_is_not_cancelled_should_be_true(FinalStateValues value)
    {
        value.IsCancelled().Should().BeFalse();
        value.IsNotCancelled().Should().BeTrue();
    }
}
