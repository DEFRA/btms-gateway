namespace BtmsGateway.Exceptions;

public class ClearanceDecisionProcessingException : Exception
{
    public ClearanceDecisionProcessingException(string message)
        : base(message) { }

    public ClearanceDecisionProcessingException(string message, Exception inner)
        : base(message, inner) { }
}
