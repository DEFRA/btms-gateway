namespace BtmsGateway.Exceptions;

public class DecisionComparisonException : Exception
{
    public DecisionComparisonException(string message) : base(message)
    {
    }

    public DecisionComparisonException(string message, Exception inner) : base(message, inner)
    {
    }
}