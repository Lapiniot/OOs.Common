using System.Net.Connections;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;

namespace System.Net.Listeners
{
    public class SslStreamTcpSocketListener : TcpSocketListenerBase
    {
        private readonly SslServerAuthenticationOptions options;

        public SslStreamTcpSocketListener(IPEndPoint endPoint,
            X509Certificate serverCertificate, int backlog = 100,
            Action<Socket> configureListening = null,
            Action<Socket> configureAccepted = null) :
            base(endPoint, backlog, configureListening, configureAccepted)
        {
            options = new SslServerAuthenticationOptions()
            {
                ServerCertificate = serverCertificate ?? throw new ArgumentNullException(nameof(serverCertificate))
            };
        }

        public override string ToString()
        {
            return $"{nameof(SslStreamTcpSocketListener)} {{tcps://{EndPoint}}}";
        }

        protected override INetworkConnection CreateConnection(Socket acceptedSocket)
        {
            return new SslStreamServerConnection(acceptedSocket, options);
        }
    }
}