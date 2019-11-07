using System.Runtime.Serialization;

namespace System.Net.Connections.Exceptions
{
    public abstract class TransportException : Exception
    {
        protected TransportException() {}

        protected TransportException(SerializationInfo info, StreamingContext context) : base(info, context) {}

        protected TransportException(string message) : base(message) {}

        protected TransportException(string message, Exception innerException) : base(message, innerException) {}
    }
}