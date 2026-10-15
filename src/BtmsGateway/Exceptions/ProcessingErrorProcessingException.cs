namespace BtmsGateway.Exceptions;

public class ProcessingErrorProcessingException : Exception
{
    public ProcessingErrorProcessingException(string message)
        : base(message) { }

    public ProcessingErrorProcessingException(string message, Exception inner)
        : base(message, inner) { }
}
