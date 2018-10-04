namespace System.Net.Transports.Exceptions
{
    public class ServerUnavailableException : TransportException
    {
        private const string ServerUnavailable = "Server unavailable.";

        public ServerUnavailableException(Exception innerException) :
            this(ServerUnavailable, innerException)
        {
        }

        public ServerUnavailableException(string message, Exception innerException) :
            base(message, innerException)
        {
        }
    }
}