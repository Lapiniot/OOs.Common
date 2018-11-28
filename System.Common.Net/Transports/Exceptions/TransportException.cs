using System.Runtime.Serialization;

namespace System.Net.Transports.Exceptions
{
    public abstract class TransportException : Exception
    {
        protected TransportException(SerializationInfo info, StreamingContext context) : base(info, context) {}

        protected TransportException(string message) : base(message) {}

        protected TransportException(string message, Exception innerException) : base(message, innerException) {}
    }
}