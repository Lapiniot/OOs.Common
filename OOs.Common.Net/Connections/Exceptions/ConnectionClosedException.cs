namespace OOs.Net.Connections.Exceptions;

public class ConnectionClosedException : TransportException
{
    protected internal const string ConnectionClosedByRemoteHost = "Connection closed by remote host";

    public ConnectionClosedException() : base(ConnectionClosedByRemoteHost) { }

    public ConnectionClosedException(string message, Exception innerException) :
        base(message, innerException)
    { }

    public ConnectionClosedException(Exception innerException) :
        base(ConnectionClosedByRemoteHost, innerException)
    { }

    public ConnectionClosedException(string message) : base(message) { }
}