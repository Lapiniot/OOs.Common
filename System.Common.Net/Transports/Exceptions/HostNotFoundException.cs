namespace System.Net.Transports.Exceptions
{
    public class HostNotFoundException : TransportException
    {
        protected internal const string HostNotFound = "Host not found.";

        public HostNotFoundException(Exception innerException) :
            base(HostNotFound, innerException) {}

        public HostNotFoundException(string message, Exception innerException) :
            base(message, innerException) {}
    }
}