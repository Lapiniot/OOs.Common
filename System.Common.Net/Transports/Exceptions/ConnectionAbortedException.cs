namespace System.Net.Transports.Exceptions
{
    public class ConnectionAbortedException : TransportException
    {
        protected internal const string ConnectionClosedByRemoteHost = "Connection closed by remote host";

        public ConnectionAbortedException(Exception innerException) :
            base(ConnectionClosedByRemoteHost, innerException)
        {
        }

        public ConnectionAbortedException(string message, Exception innerException) :
            base(message, innerException)
        {
        }
    }
}