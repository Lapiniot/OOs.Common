using System.Diagnostics.CodeAnalysis;

namespace System.Net.Transports.Exceptions
{
    [SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "Localized exception messages are not supported")]
    public class ServerUnavailableException : TransportException
    {
        private const string ServerUnavailable = "Server unavailable.";

        public ServerUnavailableException() : base(ServerUnavailable) {}

        public ServerUnavailableException(string message) : base(message) {}

        public ServerUnavailableException(Exception innerException) : 
            this(ServerUnavailable, innerException) {}

        public ServerUnavailableException(string message, Exception innerException) :
            base(message, innerException) {}
    }
}