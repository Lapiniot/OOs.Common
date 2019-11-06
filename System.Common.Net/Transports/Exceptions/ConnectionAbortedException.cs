using System.Diagnostics.CodeAnalysis;

namespace System.Net.Transports.Exceptions
{
    [SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "Localized exception messages are not supported")]
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