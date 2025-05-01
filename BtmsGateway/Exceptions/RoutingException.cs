namespace BtmsGateway.Exceptions;

public class RoutingException(string message, Exception inner) : Exception(message, inner);
