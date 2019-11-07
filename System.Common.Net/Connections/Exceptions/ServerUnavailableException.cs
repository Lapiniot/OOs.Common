namespace System.Net.Connections.Exceptions
{
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