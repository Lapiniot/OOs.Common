namespace System.Net.Connections.Exceptions
{
    public class ConnectionAbortedException : TransportException
    {
        protected internal const string ConnectionClosedByRemoteHost = "Connection closed by remote host";

        public ConnectionAbortedException() : base(ConnectionClosedByRemoteHost) {}

        public ConnectionAbortedException(string message, Exception innerException) :
            base(message, innerException) {}

        public ConnectionAbortedException(Exception innerException) :
            base(ConnectionClosedByRemoteHost, innerException) {}

        public ConnectionAbortedException(string message) : base(message) {}
    }
}