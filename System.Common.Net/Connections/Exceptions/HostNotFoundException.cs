namespace System.Net.Connections.Exceptions;

public class HostNotFoundException : TransportException
{
    protected internal const string HostNotFound = "Host not found.";

    public HostNotFoundException() : base(HostNotFound) { }

    public HostNotFoundException(string message, Exception innerException) :
        base(message, innerException)
    { }

    public HostNotFoundException(Exception innerException) :
        base(HostNotFound, innerException)
    { }

    public HostNotFoundException(string message) : base(message) { }
}