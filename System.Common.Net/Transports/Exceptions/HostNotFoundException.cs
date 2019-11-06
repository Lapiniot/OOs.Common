using System.Diagnostics.CodeAnalysis;

namespace System.Net.Transports.Exceptions
{
    [SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "Localized exception messages are not supported")]
    public class HostNotFoundException : TransportException
    {
        protected internal const string HostNotFound = "Host not found.";

        public HostNotFoundException() : base(HostNotFound) {}

        public HostNotFoundException(string message, Exception innerException) :
            base(message, innerException) {}

        public HostNotFoundException(Exception innerException) :
            base(HostNotFound, innerException) {}

        public HostNotFoundException(string message) : base(message) {}
    }
}