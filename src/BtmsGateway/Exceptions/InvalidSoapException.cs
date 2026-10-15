namespace BtmsGateway.Exceptions;

public class InvalidSoapException(string message, Exception inner) : Exception(message, inner);
